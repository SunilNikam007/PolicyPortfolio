using PolicyPortfolio;
using PolicyPortfolio.Enums;
using PolicyPortfolio.Models;
using PolicyPortfolio.Services;
using Xunit;

namespace PolicyPortfolio.Tests;

public sealed class PortfolioServiceTests
{
    private readonly PortfolioService _service = new();

    [Fact]
    public void LoadFromFile_ReturnsPortfolio_WhenJsonFileIsValid()
    {
        var json = """
        {
          "policies": [
            {
              "policyId": "POL-001",
              "holderName": "Asha Patel",
              "monthlyPremium": 1000.00,
              "status": "Active",
              "claims": []
            }
          ]
        }
        """;

        var path = CreateTempJsonFile(json);

        var result = _service.LoadFromFile(path);

        Assert.Single(result.Policies);
        Assert.Equal("POL-001", result.Policies[0].PolicyId);
        Assert.Equal(1000.00m, result.Policies[0].MonthlyPremium);
    }

    [Fact]
    public void LoadFromFile_ThrowsFileNotFoundException_WhenFileDoesNotExist()
    {
        Assert.Throws<FileNotFoundException>(() =>
            _service.LoadFromFile("missing-file.json"));
    }

    [Fact]
    public void LoadFromFile_ThrowsMeaningfulException_WhenJsonIsMalformed()
    {
        var path = CreateTempJsonFile("{ invalid json");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _service.LoadFromFile(path));

        Assert.Contains("malformed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TotalMonthlyPremium_ReturnsOnlyPoliciesMatchingRequestedStatus()
    {
        var portfolio = CreatePortfolio();

        var result = _service.TotalMonthlyPremium(portfolio, PolicyStatus.Active);

        Assert.Equal(2500m, result);
    }

    [Fact]
    public void TotalMonthlyPremium_ReturnsZero_WhenNoPolicyMatchesStatus()
    {
        var portfolio = new Portfolio
        {
            Policies =
            [
                new Policy
                {
                    PolicyId = "POL-001",
                    HolderName = "Asha",
                    MonthlyPremium = 1000m,
                    Status = PolicyStatus.Active
                }
            ]
        };

        var result = _service.TotalMonthlyPremium(portfolio, PolicyStatus.Cancelled);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void ClaimsPaidBetween_IncludesApprovedClaimsOnStartAndEndDates()
    {
        var portfolio = new Portfolio
        {
            Policies =
            [
                new Policy
                {
                    PolicyId = "POL-001",
                    Status = PolicyStatus.Active,
                    Claims =
                    [
                        ApprovedClaim("C1", 100m, new DateOnly(2024, 1, 1)),
                        ApprovedClaim("C2", 200m, new DateOnly(2024, 12, 31)),
                        ApprovedClaim("C3", 300m, new DateOnly(2025, 1, 1))
                    ]
                }
            ]
        };

        var result = _service.ClaimsPaidBetween(
            portfolio,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 12, 31));

        Assert.Equal(300m, result);
    }

    [Fact]
    public void ClaimsPaidBetween_IgnoresPendingAndRejectedClaims()
    {
        var portfolio = new Portfolio
        {
            Policies =
            [
                new Policy
                {
                    PolicyId = "POL-001",
                    Status = PolicyStatus.Active,
                    Claims =
                    [
                        ApprovedClaim("C1", 100m, new DateOnly(2024, 6, 1)),
                        PendingClaim("C2", 200m, new DateOnly(2024, 6, 1)),
                        RejectedClaim("C3", 300m, new DateOnly(2024, 6, 1))
                    ]
                }
            ]
        };

        var result = _service.ClaimsPaidBetween(
            portfolio,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 12, 31));

        Assert.Equal(100m, result);
    }

    [Fact]
    public void ClaimsPaidBetween_DoesNotFilterByOwningPolicyStatus()
    {
        var portfolio = new Portfolio
        {
            Policies =
            [
                new Policy
                {
                    PolicyId = "POL-LAPSED",
                    Status = PolicyStatus.Lapsed,
                    Claims =
                    [
                        ApprovedClaim("C1", 700m, new DateOnly(2024, 8, 1))
                    ]
                }
            ]
        };

        var result = _service.ClaimsPaidBetween(
            portfolio,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 12, 31));

        Assert.Equal(700m, result);
    }

    [Fact]
    public void ClaimsPaidBetween_ThrowsException_WhenFromDateIsAfterToDate()
    {
        var portfolio = CreatePortfolio();

        Assert.Throws<ArgumentException>(() =>
            _service.ClaimsPaidBetween(
                portfolio,
                new DateOnly(2024, 12, 31),
                new DateOnly(2024, 1, 1)));
    }

    [Fact]
    public void EvaluateClaim_ThrowsKeyNotFoundException_WhenPolicyDoesNotExist()
    {
        var portfolio = CreatePortfolio();

        Assert.Throws<KeyNotFoundException>(() =>
            _service.EvaluateClaim(
                portfolio,
                "UNKNOWN",
                IncomingClaim(500m, new DateOnly(2025, 1, 1))));
    }

    [Fact]
    public void EvaluateClaim_ReturnsReject_WhenPolicyIsLapsed()
    {
        var portfolio = CreatePortfolio();

        var result = _service.EvaluateClaim(
            portfolio,
            "POL-LAPSED",
            IncomingClaim(500m, new DateOnly(2025, 1, 1)));

        Assert.Equal(AdjudicationDecision.Reject, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsReject_WhenPolicyIsCancelled()
    {
        var portfolio = CreatePortfolio();

        var result = _service.EvaluateClaim(
            portfolio,
            "POL-CANCELLED",
            IncomingClaim(500m, new DateOnly(2025, 1, 1)));

        Assert.Equal(AdjudicationDecision.Reject, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EvaluateClaim_ReturnsReject_WhenIncomingAmountIsZeroOrNegative(decimal amount)
    {
        var portfolio = CreatePortfolio();

        var result = _service.EvaluateClaim(
            portfolio,
            "POL-ACTIVE-NO-CLAIMS",
            IncomingClaim(amount, new DateOnly(2025, 1, 1)));

        Assert.Equal(AdjudicationDecision.Reject, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsManualReview_WhenIncomingAmountIsGreaterThanTenThousand()
    {
        var portfolio = CreatePortfolio();

        var result = _service.EvaluateClaim(
            portfolio,
            "POL-ACTIVE-NO-CLAIMS",
            IncomingClaim(10_000.01m, new DateOnly(2025, 1, 1)));

        Assert.Equal(AdjudicationDecision.ManualReview, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsAutoApprove_WhenAmountIsExactlyTenThousandAndNoOtherRuleMatches()
    {
        var portfolio = CreatePortfolio();

        var result = _service.EvaluateClaim(
            portfolio,
            "POL-ACTIVE-NO-CLAIMS",
            IncomingClaim(10_000m, new DateOnly(2025, 1, 1)));

        Assert.Equal(AdjudicationDecision.AutoApprove, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsManualReview_WhenApprovedClaimExistsExactlyNinetyDaysBefore()
    {
        var incomingDate = new DateOnly(2025, 4, 1);
        var portfolio = new Portfolio
        {
            Policies =
            [
                new Policy
                {
                    PolicyId = "POL-001",
                    Status = PolicyStatus.Active,
                    Claims =
                    [
                        ApprovedClaim("OLD", 400m, incomingDate.AddDays(-90))
                    ]
                }
            ]
        };

        var result = _service.EvaluateClaim(
            portfolio,
            "POL-001",
            IncomingClaim(500m, incomingDate));

        Assert.Equal(AdjudicationDecision.ManualReview, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsAutoApprove_WhenApprovedClaimIsMoreThanNinetyDaysBefore()
    {
        var incomingDate = new DateOnly(2025, 4, 1);
        var portfolio = new Portfolio
        {
            Policies =
            [
                new Policy
                {
                    PolicyId = "POL-001",
                    Status = PolicyStatus.Active,
                    Claims =
                    [
                        ApprovedClaim("OLD", 400m, incomingDate.AddDays(-91))
                    ]
                }
            ]
        };

        var result = _service.EvaluateClaim(
            portfolio,
            "POL-001",
            IncomingClaim(500m, incomingDate));

        Assert.Equal(AdjudicationDecision.AutoApprove, result);
    }

    [Fact]
    public void EvaluateClaim_IgnoresPendingAndRejectedClaimsWithinNinetyDays()
    {
        var incomingDate = new DateOnly(2025, 4, 1);
        var portfolio = new Portfolio
        {
            Policies =
            [
                new Policy
                {
                    PolicyId = "POL-001",
                    Status = PolicyStatus.Active,
                    Claims =
                    [
                        PendingClaim("PENDING", 400m, incomingDate.AddDays(-10)),
                        RejectedClaim("REJECTED", 400m, incomingDate.AddDays(-20))
                    ]
                }
            ]
        };

        var result = _service.EvaluateClaim(
            portfolio,
            "POL-001",
            IncomingClaim(500m, incomingDate));

        Assert.Equal(AdjudicationDecision.AutoApprove, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsAutoApprove_WhenNoRejectOrManualReviewRulesMatch()
    {
        var portfolio = CreatePortfolio();

        var result = _service.EvaluateClaim(
            portfolio,
            "POL-ACTIVE-NO-CLAIMS",
            IncomingClaim(500m, new DateOnly(2025, 1, 1)));

        Assert.Equal(AdjudicationDecision.AutoApprove, result);
    }

    [Fact]
    public void EvaluateClaim_RejectPriorityBeatsManualReview_WhenPolicyIsLapsedAndAmountIsLarge()
    {
        var portfolio = CreatePortfolio();

        var result = _service.EvaluateClaim(
            portfolio,
            "POL-LAPSED",
            IncomingClaim(50_000m, new DateOnly(2025, 1, 1)));

        Assert.Equal(AdjudicationDecision.Reject, result);
    }

    private static Portfolio CreatePortfolio()
    {
        return new Portfolio
        {
            Policies =
            [
                new Policy
                {
                    PolicyId = "POL-ACTIVE-NO-CLAIMS",
                    HolderName = "Asha Patel",
                    MonthlyPremium = 1000m,
                    Status = PolicyStatus.Active,
                    Claims = []
                },
                new Policy
                {
                    PolicyId = "POL-ACTIVE-WITH-CLAIMS",
                    HolderName = "Ravi Sharma",
                    MonthlyPremium = 1500m,
                    Status = PolicyStatus.Active,
                    Claims =
                    [
                        ApprovedClaim("C1", 500m, new DateOnly(2024, 3, 1))
                    ]
                },
                new Policy
                {
                    PolicyId = "POL-LAPSED",
                    HolderName = "John Smith",
                    MonthlyPremium = 800m,
                    Status = PolicyStatus.Lapsed,
                    Claims = []
                },
                new Policy
                {
                    PolicyId = "POL-CANCELLED",
                    HolderName = "Mary Jones",
                    MonthlyPremium = 700m,
                    Status = PolicyStatus.Cancelled,
                    Claims = []
                }
            ]
        };
    }

    private static Claim IncomingClaim(decimal amount, DateOnly dateRaised)
    {
        return new Claim
        {
            ClaimId = "INCOMING",
            Amount = amount,
            Status = ClaimStatus.Pending,
            DateRaised = dateRaised
        };
    }

    private static Claim ApprovedClaim(string claimId, decimal amount, DateOnly dateRaised)
    {
        return new Claim
        {
            ClaimId = claimId,
            Amount = amount,
            Status = ClaimStatus.Approved,
            DateRaised = dateRaised
        };
    }

    private static Claim PendingClaim(string claimId, decimal amount, DateOnly dateRaised)
    {
        return new Claim
        {
            ClaimId = claimId,
            Amount = amount,
            Status = ClaimStatus.Pending,
            DateRaised = dateRaised
        };
    }

    private static Claim RejectedClaim(string claimId, decimal amount, DateOnly dateRaised)
    {
        return new Claim
        {
            ClaimId = claimId,
            Amount = amount,
            Status = ClaimStatus.Rejected,
            DateRaised = dateRaised
        };
    }

    private static string CreateTempJsonFile(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
