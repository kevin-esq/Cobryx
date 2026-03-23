using Cobryx.Domain.ML;

namespace Cobryx.Application.ML;

public class MonteCarloMetrics
{
    public decimal AverageCreditMultiplier { get; set; }

    public decimal VaR95CreditMultiplier { get; set; }

    public decimal ExpectedShortfallCredit { get; set; }

    public decimal AverageInterestDelta { get; set; }
    public decimal VaR95InterestDelta { get; set; }

    public decimal[] RawCreditMultipliers { get; set; } = [];
    public decimal[] RawInterestDeltas { get; set; } = [];
}

public class MonteCarloEvaluator(MonteCarloPpoClient ppoClient)
{
    public async Task<MonteCarloMetrics> EvaluateAsync(
        object features,
        PortfolioState globalState,
        MacroState currentMacro,
        List<Scenario> scenarios)
    {
        var response = await ppoClient.EvaluateBatchAsync(features, globalState, currentMacro, scenarios);

        var credits = response.CreditMultipliers.OrderBy(x => x).ToList();
        var rates = response.InterestDeltas.OrderByDescending(x => x).ToList();

        int tailSize = (int)(0.05 * credits.Count);
        if (tailSize == 0 && credits.Count > 0) tailSize = 1;
        else if (credits.Count == 0) tailSize = 0;

        return new MonteCarloMetrics
        {
            AverageCreditMultiplier = credits.Count > 0 ? credits.Average() : 1.0m,
            VaR95CreditMultiplier = tailSize > 0 ? credits[tailSize] : 1.0m,
            ExpectedShortfallCredit = tailSize > 0 ? credits.Take(tailSize).Average() : 1.0m,

            AverageInterestDelta = rates.Count > 0 ? rates.Average() : 0.0m,
            VaR95InterestDelta = tailSize > 0 ? rates[tailSize] : 0.0m,

            RawCreditMultipliers = credits.ToArray(),
            RawInterestDeltas = rates.ToArray()
        };
    }
}
