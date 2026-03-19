using System.Text.Json;
using Cobryx.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.ML.Jobs;

public class PpoTrainingJob(
    ICobryxDbContext db,
    MonteCarloPpoClient ppo)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task RunAsync()
    {
        // 1. Intelligent Sampling (Hybrid Learning)
        var simulationBatch = await db.Experiences
            .Where(x => x.Source == "simulation" && (x.Done || x.Reward != 0))
            .OrderByDescending(x => x.Reward < -1000 ? 1 : 0) // Priority to high loss/crisis
            .Take(512)
            .ToListAsync();

        var prodBatch = await db.Experiences
            .Where(x => x.Source == "production" && x.Reward != 0)
            .Take(307)
            .ToListAsync();

        var replayBatch = await db.Experiences
            .Where(x => x.Source == "replay" && x.Reward != 0)
            .Take(205)
            .ToListAsync();

        var hybridBatch = simulationBatch.Concat(prodBatch).Concat(replayBatch).ToList();

        if (hybridBatch.Count == 0) return;

        // 2. Format payload for Python API
        var payload = hybridBatch.Select(x => new
        {
            state = JsonSerializer.Deserialize<object>(x.StateJson, JsonOptions),
            action = new { creditMultiplier = x.CreditMultiplier, interestDelta = x.InterestDelta },
            reward = x.Reward,
            done = x.Done
        });

        // 3. Trigger Training Pipeline
        await ppo.TrainAsync(payload);
    }
}
