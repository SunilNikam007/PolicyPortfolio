using PolicyPortfolio.Enums;

namespace PolicyPortfolio.Models;

public class Claim
{
    public string ClaimId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public ClaimStatus Status { get; init; }
    public DateOnly DateRaised { get; init; }
}


