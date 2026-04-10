using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.ML.Interfaces;
using Cobryx.Domain.ML;
using Cobryx.Domain.ML.Simulation;

namespace Cobryx.Application.ML.Simulation;

public class SimulationRunner(
    IMonteCarloPpoClient ppo,
    EconomyEnvironment env,
    ICobryxDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task RunEpisodeAsync(int steps)
    {
        var state = env.Reset();

        for (int t = 0; t < steps; t++)
        {
            var decision = await ppo.DecideAsync(state);

            var creditMultiplier = decision.CreditMultiplier;
            if (creditMultiplier > 3.0m)
                creditMultiplier = 3.0m;
            if (env.Macro.Regime == MarketRegime.Crisis)
                creditMultiplier = Math.Min(creditMultiplier, 0.5m);

            var action = new EconomyAction { CreditMultiplier = creditMultiplier, InterestDelta = decision.InterestDelta };
            var result = env.Step(action);

            db.Experiences.Add(new Experience
            {
                CustomerId = Guid.Empty,
                StateJson = JsonSerializer.Serialize(state, JsonOptions),
                CreditMultiplier = action.CreditMultiplier,
                InterestDelta = action.InterestDelta,
                LogProb = decision.LogProb,
                Value = decision.Value,
                Reward = result.Reward,
                NextStateJson = JsonSerializer.Serialize(result.NextState, JsonOptions),
                Done = result.Done,
                Source = "simulation"
            });

            state = result.NextState;

            if (result.Done)
                break;
        }

        await db.SaveChangesAsync(default);
    }
}
