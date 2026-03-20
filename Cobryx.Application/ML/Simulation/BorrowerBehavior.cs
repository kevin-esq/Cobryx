using Cobryx.Domain.ML;
using Cobryx.Domain.ML.Simulation;

namespace Cobryx.Application.ML.Simulation;

public static class BorrowerBehavior
{
    public static bool WillDefault(SimulatedCustomer c, MacroState macro, Random rng)
    {
        var baseRisk = 1m - c.CreditScore;

        var macroStress =
            macro.Inflation * 0.3m +
            macro.InterestRate * 0.4m +
            macro.Unemployment * 0.3m;

        var prob = baseRisk + macroStress;

        return rng.NextDouble() < (double)prob;
    }
}
