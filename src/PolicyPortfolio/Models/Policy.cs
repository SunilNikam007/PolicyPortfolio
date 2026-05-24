using PolicyPortfolio.Enums;

namespace PolicyPortfolio.Models;

public class Policy
{
    public string PolicyId { get; init; } = string.Empty;
    public string HolderName { get; init; } = string.Empty;
    public decimal MonthlyPremium { get; init; }
    public PolicyStatus Status { get; init; }
    public List<Claim> Claims { get; set; } = [];
}


