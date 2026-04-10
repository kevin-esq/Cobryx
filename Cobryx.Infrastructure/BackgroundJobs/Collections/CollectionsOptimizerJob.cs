using Cobryx.Application.Collections.Optimizer;
using Cobryx.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Cobryx.Infrastructure.BackgroundJobs.Collections;

public partial class CollectionsOptimizerJob(
    ICollectionOptimizer optimizer,
    ICobryxDbContext dbContext,
    IConnectionMultiplexer redis,
    ILogger<CollectionsOptimizerJob> logger)
{
    public async Task RunHourlyOptimizationAsync()
    {
        LogJobStarted(logger);
        var activeTenants = await dbContext.CollectionOutcomes
            .Select(o => o.TenantId)
            .Distinct()
            .ToListAsync();

        var db = redis.GetDatabase();
        var lockKey = "portfolio:collections:optimizer:lock";
        var token = Guid.NewGuid().ToString();

        var acquired = await db.LockTakeAsync(lockKey, token, TimeSpan.FromMinutes(5));
        if (!acquired)
        {
            LogOptimizerAlreadyRunning(logger);
            return;
        }

        try
        {
            foreach (var tenantId in activeTenants)
            {
                try
                {
                    var weights = await optimizer.CalculateWeightsAsync(tenantId);
                    var key = $"portfolio:collections:weights:{tenantId}";
                    var json = System.Text.Json.JsonSerializer.Serialize(weights);

                    var tran = db.CreateTransaction();
                    _ = tran.KeyDeleteAsync(key);
                    _ = tran.StringSetAsync(key, json, TimeSpan.FromDays(1));
                    await tran.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    LogOptimizationFailed(logger, ex, tenantId);
                }
            }
        }
        finally
        {
            await db.LockReleaseAsync(lockKey, token);
        }
        LogJobCompleted(logger);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Starting ML Optimizer Job...")]
    static partial void LogJobStarted(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Optimizer is already running on another instance, skipping.")]
    static partial void LogOptimizerAlreadyRunning(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "ML Optimizer Job completed.")]
    static partial void LogJobCompleted(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Optimization failed for tenant {TenantId}.")]
    static partial void LogOptimizationFailed(ILogger logger, Exception ex, Guid tenantId);
}
