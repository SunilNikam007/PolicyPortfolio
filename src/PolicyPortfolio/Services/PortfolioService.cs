using PolicyPortfolio.Converters;
using PolicyPortfolio.Enums;
using PolicyPortfolio.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolicyPortfolio.Services;

/// <summary>
/// Implement the four required methods below. You may add additional
/// helper methods or classes as needed, but you may NOT change the
/// public signatures of the four methods declared here.
/// </summary>
public class PortfolioService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(),
            new DateOnlyJsonConverter()
        }
    };

    /// <summary>
    /// Loads a <see cref="Portfolio"/> from a JSON file at the given path.
    /// Throw an appropriate exception if the file is missing or malformed.
    /// </summary>
    public Portfolio LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Portfolio JSON file path must be provided.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Portfolio JSON file was not found: {path}", path);

        try
        {
            var json = File.ReadAllText(path);
            var portfolio = JsonSerializer.Deserialize<Portfolio>(json, JsonOptions);

            if (portfolio is null)
                throw new InvalidOperationException("Portfolio JSON file is empty or could not be deserialised.");

            portfolio.Policies ??= [];

            foreach (var policy in portfolio.Policies)
            {
                policy.Claims ??= [];
            }

            return portfolio;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Portfolio JSON file is malformed or does not match the expected portfolio format.",
                ex);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Could not read portfolio JSON file: {path}",
                ex);
        }
    }

    /// <summary>
    /// Returns the sum of MonthlyPremium for all policies whose Status
    /// matches the supplied filter.
    /// </summary>
    public decimal TotalMonthlyPremium(Portfolio portfolio, PolicyStatus status)
    {
        ArgumentNullException.ThrowIfNull(portfolio);

        return portfolio.Policies
            .Where(policy => policy.Status == status)
            .Sum(policy => policy.MonthlyPremium);
    }

    /// <summary>
    /// Returns the sum of Amount for all Approved claims (regardless of the
    /// owning policy's status) whose DateRaised falls within the inclusive
    /// range [fromInclusive, toInclusive], across all policies.
    /// </summary>
    public decimal ClaimsPaidBetween(Portfolio portfolio, DateOnly fromInclusive, DateOnly toInclusive)
    {
        ArgumentNullException.ThrowIfNull(portfolio);

        if (fromInclusive > toInclusive)
            throw new ArgumentException("From date must be before or equal to To date.");

        return portfolio.Policies
            .SelectMany(policy => policy.Claims)
            .Where(claim =>
                claim.Status == ClaimStatus.Approved &&
                claim.DateRaised >= fromInclusive &&
                claim.DateRaised <= toInclusive)
            .Sum(claim => claim.Amount);
    }

    /// <summary>
    /// Decides what should happen to an incoming claim against the named policy.
    /// Apply the rules from the brief in priority order: Reject beats
    /// ManualReview beats AutoApprove. Throw an appropriate exception if no
    /// policy with that Id exists. The Status of the incoming claim is not used
    /// — your method is deciding what its new status should be.
    /// </summary>
    public AdjudicationDecision EvaluateClaim(Portfolio portfolio, string policyId, Claim incomingClaim)
    {
        ArgumentNullException.ThrowIfNull(portfolio);
        ArgumentNullException.ThrowIfNull(incomingClaim);

        if (string.IsNullOrWhiteSpace(policyId))
            throw new ArgumentException("Policy ID must be provided.", nameof(policyId));

        var policy = portfolio.Policies
            .SingleOrDefault(policy => policy.PolicyId == policyId);

        if (policy is null)
            throw new KeyNotFoundException($"No policy exists with ID '{policyId}'.");

        if (policy.Status is PolicyStatus.Lapsed or PolicyStatus.Cancelled)
            return AdjudicationDecision.Reject;

        if (incomingClaim.Amount <= 0m)
            return AdjudicationDecision.Reject;

        if (incomingClaim.Amount > 10_000m)
            return AdjudicationDecision.ManualReview;

        var earliestDateIncluded = incomingClaim.DateRaised.AddDays(-90);

        var hasApprovedClaimWithinPrevious90Days = policy.Claims.Any(claim =>
            claim.Status == ClaimStatus.Approved &&
            claim.DateRaised >= earliestDateIncluded &&
            claim.DateRaised < incomingClaim.DateRaised);

        if (hasApprovedClaimWithinPrevious90Days)
            return AdjudicationDecision.ManualReview;

        return AdjudicationDecision.AutoApprove;
    }
}
