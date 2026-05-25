using PolicyPortfolio.Enums;
using PolicyPortfolio.Models;
using PolicyPortfolio.Services;
using Xunit;

namespace PolicyPortfolio.Tests;

public class PortfolioServiceJsonDataTests
{
    private readonly PortfolioService _service = new();

    private static Portfolio CreateSamplePortfolio()
    {
        return new Portfolio
        {
            Policies =
            [
                new Policy
                {
                    PolicyId = "POL-001",
                    HolderName = "Jane Smith",
                    MonthlyPremium = 450.00m,
                    Status = PolicyStatus.Active,
                    Claims =
                    [
                        new Claim { ClaimId = "CLM-1001", Amount = 1200.00m, Status = ClaimStatus.Approved, DateRaised = new DateOnly(2024, 3, 15) },
                        new Claim { ClaimId = "CLM-1002", Amount = 800.00m, Status = ClaimStatus.Pending, DateRaised = new DateOnly(2024, 6, 22) },
                        new Claim { ClaimId = "CLM-1003", Amount = 500.00m, Status = ClaimStatus.Approved, DateRaised = new DateOnly(2024, 1, 1) },
                        new Claim { ClaimId = "CLM-1004", Amount = 300.00m, Status = ClaimStatus.Approved, DateRaised = new DateOnly(2024, 12, 31) }
                    ]
                },
                new Policy
                {
                    PolicyId = "POL-002",
                    HolderName = "Peter van der Merwe",
                    MonthlyPremium = 320.50m,
                    Status = PolicyStatus.Lapsed,
                    Claims =
                    [
                        new Claim { ClaimId = "CLM-2001", Amount = 1500.00m, Status = ClaimStatus.Approved, DateRaised = new DateOnly(2024, 8, 10) }
                    ]
                },
                new Policy
                {
                    PolicyId = "POL-003",
                    HolderName = "Aaliyah Patel",
                    MonthlyPremium = 675.75m,
                    Status = PolicyStatus.Active,
                    Claims = []
                },
                new Policy
                {
                    PolicyId = "POL-004",
                    HolderName = "David Okafor",
                    MonthlyPremium = 220.00m,
                    Status = PolicyStatus.Cancelled,
                    Claims =
                    [
                        new Claim { ClaimId = "CLM-4001", Amount = 200.00m, Status = ClaimStatus.Rejected, DateRaised = new DateOnly(2024, 5, 18) }
                    ]
                }
            ]
        };
    }

    [Fact]
    public void TotalMonthlyPremium_WhenStatusIsActive()
    {
        var portfolio = CreateSamplePortfolio();

        var result = _service.TotalMonthlyPremium(portfolio, PolicyStatus.Active);

        Assert.Equal(1125.75m, result);
    }

    [Fact]
    public void TotalMonthlyPremium_WhenStatusIsLapsed()
    {
        var portfolio = CreateSamplePortfolio();

        var result = _service.TotalMonthlyPremium(portfolio, PolicyStatus.Lapsed);

        Assert.Equal(320.50m, result);
    }

    [Fact]
    public void TotalMonthlyPremium_WhenStatusIsCancelled()
    {
        var portfolio = CreateSamplePortfolio();

        var result = _service.TotalMonthlyPremium(portfolio, PolicyStatus.Cancelled);

        Assert.Equal(220.00m, result);
    }

    [Fact]
    public void ClaimsPaidBetween_ForApprovedClaims()
    {
        var portfolio = CreateSamplePortfolio();

        var result = _service.ClaimsPaidBetween(
            portfolio,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 12, 31));

        Assert.Equal(3500.00m, result);
    }

    [Fact]
    public void ClaimsPaidBetween_IncludesStartAndEndDateClaims()
    {
        var portfolio = CreateSamplePortfolio();

        var result = _service.ClaimsPaidBetween(
            portfolio,
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 12, 31));

        Assert.Equal(3500.00m, result);
    }

    [Fact]
    public void ClaimsPaidBetween_IgnoresPendingAndRejectedClaims()
    {
        var portfolio = CreateSamplePortfolio();

        var result = _service.ClaimsPaidBetween(
            portfolio,
            new DateOnly(2024, 5, 1),
            new DateOnly(2024, 6, 30));

        Assert.Equal(0m, result);
    }

    [Fact]
    public void ClaimsPaidBetween_DoesNotFilterByPolicyStatus()
    {
        var portfolio = CreateSamplePortfolio();

        var result = _service.ClaimsPaidBetween(
            portfolio,
            new DateOnly(2024, 8, 1),
            new DateOnly(2024, 8, 31));

        Assert.Equal(1500.00m, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsAutoApprove_ForActivePolicyWithNoPriorApprovedClaimWithin90Days()
    {
        var portfolio = CreateSamplePortfolio();

        var incomingClaim = new Claim
        {
            ClaimId = "NEW-001",
            Amount = 1000m,
            Status = ClaimStatus.Pending,
            DateRaised = new DateOnly(2025, 4, 1)
        };

        var result = _service.EvaluateClaim(portfolio, "POL-003", incomingClaim);

        Assert.Equal(AdjudicationDecision.AutoApprove, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsManualReview_WhenAmountIsGreaterThan10000()
    {
        var portfolio = CreateSamplePortfolio();

        var incomingClaim = new Claim
        {
            ClaimId = "NEW-002",
            Amount = 10000.01m,
            Status = ClaimStatus.Pending,
            DateRaised = new DateOnly(2025, 4, 1)
        };

        var result = _service.EvaluateClaim(portfolio, "POL-003", incomingClaim);

        Assert.Equal(AdjudicationDecision.ManualReview, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsReject_WhenPolicyIsLapsed()
    {
        var portfolio = CreateSamplePortfolio();

        var incomingClaim = new Claim
        {
            ClaimId = "NEW-003",
            Amount = 1000m,
            Status = ClaimStatus.Pending,
            DateRaised = new DateOnly(2025, 4, 1)
        };

        var result = _service.EvaluateClaim(portfolio, "POL-002", incomingClaim);

        Assert.Equal(AdjudicationDecision.Reject, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsReject_WhenPolicyIsCancelled()
    {
        var portfolio = CreateSamplePortfolio();

        var incomingClaim = new Claim
        {
            ClaimId = "NEW-004",
            Amount = 1000m,
            Status = ClaimStatus.Pending,
            DateRaised = new DateOnly(2025, 4, 1)
        };

        var result = _service.EvaluateClaim(portfolio, "POL-004", incomingClaim);

        Assert.Equal(AdjudicationDecision.Reject, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsReject_WhenAmountIsZero()
    {
        var portfolio = CreateSamplePortfolio();

        var incomingClaim = new Claim
        {
            ClaimId = "NEW-005",
            Amount = 0m,
            Status = ClaimStatus.Pending,
            DateRaised = new DateOnly(2025, 4, 1)
        };

        var result = _service.EvaluateClaim(portfolio, "POL-003", incomingClaim);

        Assert.Equal(AdjudicationDecision.Reject, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsReject_WhenAmountIsNegative()
    {
        var portfolio = CreateSamplePortfolio();

        var incomingClaim = new Claim
        {
            ClaimId = "NEW-006",
            Amount = -100m,
            Status = ClaimStatus.Pending,
            DateRaised = new DateOnly(2025, 4, 1)
        };

        var result = _service.EvaluateClaim(portfolio, "POL-003", incomingClaim);

        Assert.Equal(AdjudicationDecision.Reject, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsManualReview_WhenApprovedClaimExistsWithin90Days()
    {
        var portfolio = CreateSamplePortfolio();

        var incomingClaim = new Claim
        {
            ClaimId = "NEW-007",
            Amount = 1000m,
            Status = ClaimStatus.Pending,
            DateRaised = new DateOnly(2024, 5, 1)
        };

        var result = _service.EvaluateClaim(portfolio, "POL-001", incomingClaim);

        Assert.Equal(AdjudicationDecision.ManualReview, result);
    }

    [Fact]
    public void EvaluateClaim_ReturnsAutoApprove_WhenOnlyPendingClaimExistsWithin90Days()
    {
        var portfolio = CreateSamplePortfolio();

        var incomingClaim = new Claim
        {
            ClaimId = "NEW-008",
            Amount = 1000m,
            Status = ClaimStatus.Pending,
            DateRaised = new DateOnly(2024, 9, 1)
        };

        var result = _service.EvaluateClaim(portfolio, "POL-001", incomingClaim);

        Assert.Equal(AdjudicationDecision.AutoApprove, result);
    }

    [Fact]
    public void EvaluateClaim_ThrowsException_WhenPolicyDoesNotExist()
    {
        var portfolio = CreateSamplePortfolio();

        var incomingClaim = new Claim
        {
            ClaimId = "NEW-009",
            Amount = 1000m,
            Status = ClaimStatus.Pending,
            DateRaised = new DateOnly(2025, 4, 1)
        };

        Assert.Throws<KeyNotFoundException>(() =>
            _service.EvaluateClaim(portfolio, "POL-999", incomingClaim));
    }
}