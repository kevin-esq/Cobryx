using Cobryx.Domain.ML;
using Cobryx.Domain.ML.Simulation;

namespace Cobryx.Application.ML.Simulation;

public class EconomyEnvironment(int seed)
{
    public MacroState Macro { get; set; } = new();
    public PortfolioState Portfolio { get; set; } = new();
    public List<SimulatedCustomer> Customers { get; set; } = new();

    private readonly Random _rng = new(seed);
    private readonly RegimeEngine _regimeEngine = new(seed);

    public EconomyState Reset()
    {
        Customers = GeneratePopulation(1000);
        Portfolio = new PortfolioState();
        Macro = new MacroState { Inflation = 0.05m, InterestRate = 0.05m, Unemployment = 0.05m };

        return BuildState();
    }

    public StepResult Step(EconomyAction action)
    {
        decimal totalProfit = 0;
        decimal totalLoss = 0;

        foreach (var c in Customers.Where(x => !x.IsDefaulted))
        {
            // Apply RL Action
            var credit = c.Outstanding * action.CreditMultiplier;
            var interest = 0.2m + action.InterestDelta;

            // Updated Exposure
            c.Outstanding += credit;

            // Borrower Behavior Simulation
            if (BorrowerBehavior.WillDefault(c, Macro, _rng))
            {
                c.IsDefaulted = true;
                totalLoss += c.Outstanding;
            }
            else
            {
                var payment = c.Outstanding * interest;
                totalProfit += payment;
                c.Outstanding -= payment * 0.5m;
            }
        }

        // Global Portfolio Update
        Portfolio.TotalExposure = Customers.Sum(x => x.Outstanding);
        Portfolio.DefaultRate =
            Customers.Count == 0 ? 0 : Customers.Count(x => x.IsDefaulted) / (decimal)Customers.Count;

        // Macro Dynamics Step with Regime
        Macro.Regime = _regimeEngine.Next();

        if (Macro.Regime == MarketRegime.Crisis)
        {
            Macro.Inflation += 0.05m;
            Macro.InterestRate += 0.05m;
            Macro.Unemployment += 0.05m;
        }
        else if (Macro.Regime == MarketRegime.Recovery)
        {
            Macro.Inflation -= 0.02m;
            Macro.InterestRate -= 0.01m;
            Macro.Unemployment -= 0.02m;
        }
        else
        {
            Macro.Inflation += ((decimal)_rng.NextDouble() - 0.5m) * 0.01m;
            Macro.InterestRate += ((decimal)_rng.NextDouble() - 0.5m) * 0.01m;
            Macro.Unemployment += ((decimal)_rng.NextDouble() - 0.5m) * 0.01m;
        }

        var reward = ComputeReward(totalProfit, totalLoss);

        return new StepResult
        {
            Reward = reward,
            Done = false, // Add logic if max episodes or mass default
            NextState = BuildState()
        };
    }

    private decimal ComputeReward(decimal profit, decimal loss)
    {
        var baseReward = profit - loss;

        var regimeMultiplier = Macro.Regime switch
        {
            MarketRegime.Normal => 1.0m,
            MarketRegime.HighInflation => 0.7m,
            MarketRegime.Crisis => 0.4m,
            MarketRegime.Recovery => 1.2m,
            _ => 1.0m
        };

        var riskPenalty = Macro.Regime switch
        {
            MarketRegime.Crisis => Portfolio.TotalExposure * 0.05m,
            MarketRegime.HighInflation => Portfolio.TotalExposure * 0.02m,
            _ => 0
        };

        var liquidityPenalty = Portfolio.AvailableLiquidity < 100000m
            ? 30000m
            : 0;

        return (baseReward * regimeMultiplier)
               - riskPenalty
               - liquidityPenalty;
    }

    private EconomyState BuildState()
    {
        var activeCustomers = Customers.Where(c => !c.IsDefaulted).ToList();
        var avgPd = activeCustomers.Count > 0 ? activeCustomers.Average(c => 1m - c.CreditScore) : 1m;

        return new EconomyState
        {
            AvgPd = avgPd,
            Exposure = Portfolio.TotalExposure,
            Liquidity = 5000000m - Portfolio.TotalExposure, // Stub
            Inflation = Macro.Inflation,
            InterestRate = Macro.InterestRate,
            Unemployment = Macro.Unemployment,
            Regime = Macro.Regime
        };
    }

    private List<SimulatedCustomer> GeneratePopulation(int size)
    {
        var pop = new List<SimulatedCustomer>();
        for (int i = 0; i < size; i++)
        {
            pop.Add(new SimulatedCustomer
            {
                Id = Guid.NewGuid(),
                CreditScore = 0.5m + ((decimal)_rng.NextDouble() * 0.4m), // 0.5 - 0.9
                Income = 1000m + ((decimal)_rng.NextDouble() * 9000m),
                Utilization = (decimal)_rng.NextDouble(),
                Outstanding = 500m + ((decimal)_rng.NextDouble() * 2000m),
                IsDefaulted = false,
                RiskTolerance = (decimal)_rng.NextDouble()
            });
        }

        return pop;
    }
}
