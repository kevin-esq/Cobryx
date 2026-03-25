using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.ML;

namespace Cobryx.Application.ML;

public class ScenarioGenerator(IRandomProvider rng)
{
    private readonly IRandomProvider _rng = rng;

    public List<Scenario> Generate(MacroState macro, int n = 50)
    {
        var scenarios = new List<Scenario>();

        for (int i = 0; i < n; i++)
        {
            scenarios.Add(new Scenario
            {
                Inflation = Math.Max(0m, macro.Inflation + RandomShock(0.04m)),
                InterestRate = Math.Max(0m, macro.InterestRate + RandomShock(0.03m)),
                DefaultRate = 0.05m + RandomShock(0.05m),
                LiquidityShock = RandomShock(0.2m)
            });
        }

        return scenarios;
    }

    public List<Scenario> GetStressTestScenarios()
    {
        return new List<Scenario>
        {
            new Scenario
            {
                Inflation = 0.15m, InterestRate = 0.20m, DefaultRate = 0.30m, LiquidityShock = -0.5m
            },
            new Scenario
            {
                Inflation = -0.02m, InterestRate = 0.01m, DefaultRate = 0.15m, LiquidityShock = -0.8m
            }
        };
    }

    private decimal RandomShock(decimal scale)
    {
        return (decimal)(_rng.NextDouble() - 0.5) * scale;
    }
}
