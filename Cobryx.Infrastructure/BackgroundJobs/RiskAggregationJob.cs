using Cobryx.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs;

public class RiskAggregationJob(
    ICobryxDbContext db,
    ICacheService cache,
    ILogger<RiskAggregationJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Aggregating Risk Metrics (VaR / CVaR) from Postgres to Redis.");

        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var logs = await db.DecisionDistributionLogs
            .AsNoTracking()
            .Where(x => x.Timestamp > oneHourAgo)
            .ToListAsync(ct);

        if (!logs.Any()) return;

        var allMultipliers = logs.SelectMany(x => x.CreditMultipliers).OrderBy(x => x).ToList();

        if (!allMultipliers.Any()) return;

        int tailSize = (int)(0.05 * allMultipliers.Count);
        if (tailSize == 0) tailSize = 1;

        var globalVar95 = allMultipliers[tailSize];
        var globalCVar95 = allMultipliers.Take(tailSize).Average();

        await cache.SetAsync("risk:var:1h", globalVar95, TimeSpan.FromMinutes(60), ct);
        await cache.SetAsync("risk:cvar:1h", globalCVar95, TimeSpan.FromMinutes(60), ct);

        var averageMultiplier = allMultipliers.Average();
        await cache.SetAsync("risk:exposure_multiplier_avg:1h", averageMultiplier, TimeSpan.FromMinutes(60), ct);

        logger.LogInformation("Risk Aggregation Completed. VaR95: {VaR}, CVaR95: {CVaR}", globalVar95, globalCVar95);
    }
}
