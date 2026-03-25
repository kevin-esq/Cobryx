using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.ML;
using Cobryx.Domain.ML.Simulation;

namespace Cobryx.Application.ML.Simulation;

public class SimulationRunner(
    MonteCarloPpoClient ppo,
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

            var action = new EconomyAction
            {
                CreditMultiplier = decision.CreditMultiplier,
                InterestDelta = decision.InterestDelta
            };

            if (action.CreditMultiplier > 3.0m)
                action.CreditMultiplier = 3.0m;

            if (env.Macro.Regime == MarketRegime.Crisis)
            {
                action.CreditMultiplier = Math.Min(action.CreditMultiplier, 0.5m);
            }

            var result = env.Step(action);

            db.Experiences.Add(new Experience
            {
                CustomerId = Guid.Empty,
                StateJson = JsonSerializer.Serialize(state, JsonOptions),
                NextStateJson = JsonSerializer.Serialize(result.NextState, JsonOptions),
                CreditMultiplier = action.CreditMultiplier,
                InterestDelta = action.InterestDelta,
                Reward = result.Reward,
                LogProb = decision.LogProb,
                Value = decision.Value,
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
