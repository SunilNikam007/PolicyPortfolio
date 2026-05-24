using PolicyPortfolio.Enums;
using PolicyPortfolio.Models;
using PolicyPortfolio.Services;

namespace PolicyPortfolio.ConsoleApp;

internal class Program
{
    private static int Main(string[] args)
    {   
        var service = new PortfolioService();
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide the portfolio JSON file path.");
            Console.WriteLine("Example:");
            Console.WriteLine("dotnet run --project src/PolicyPortfolio.Console data/sample-portfolio.json");
            return 0;
        }

        try
        {
            var path = args[0];
            var portfolio = service.LoadFromFile(args[0]);

            Console.WriteLine("Alula Policy Portfolio Demo");
            Console.WriteLine("---------------------------");

            Console.WriteLine($"Total monthly premium for ACTIVE policies : R {service.TotalMonthlyPremium(portfolio, PolicyStatus.Active):N2}");
            Console.WriteLine($"Total monthly premium for LAPSED policies : R {service.TotalMonthlyPremium(portfolio, PolicyStatus.Lapsed):N2}");

            var claimsPaidIn2024 = service.ClaimsPaidBetween(
                portfolio,
                new DateOnly(2024, 1, 1),
                new DateOnly(2024, 12, 31));

            Console.WriteLine($"Claims paid between 2024-01-01 and 2024-12-31 : R {claimsPaidIn2024:N2}");

            Console.WriteLine();
            Console.WriteLine("Claim adjudication examples");
            Console.WriteLine("---------------------------");

            PrintDecision(service, portfolio, "POL-003", new Claim
            {
                ClaimId = "NEW-AUTO",
                Amount = 750m,
                Status = ClaimStatus.Pending,
                DateRaised = new DateOnly(2025, 4, 1)
            });

            PrintDecision(service, portfolio, "POL-001", new Claim
            {
                ClaimId = "NEW-MANUAL",
                Amount = 12_000m,
                Status = ClaimStatus.Pending,
                DateRaised = new DateOnly(2025, 4, 1)
            });

            PrintDecision(service, portfolio, "POL-002", new Claim
            {
                ClaimId = "NEW-REJECT",
                Amount = 500m,
                Status = ClaimStatus.Pending,
                DateRaised = new DateOnly(2025, 4, 1)
            });          
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        return 1; 
    }

static void PrintDecision(
    PortfolioService service,
    Portfolio portfolio,
    string policyId,
    Claim incomingClaim)
    {
        var decision = service.EvaluateClaim(portfolio, policyId, incomingClaim);

        Console.WriteLine(
            $"Policy: {policyId}, Claim: {incomingClaim.ClaimId}, Amount: R {incomingClaim.Amount:N2}, Decision: {decision}");
    }
}
