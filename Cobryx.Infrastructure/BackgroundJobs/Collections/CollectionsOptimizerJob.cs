using Cobryx.Application.Collections.Optimizer;
using Cobryx.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Cobryx.Infrastructure.BackgroundJobs.Collections;

public class CollectionsOptimizerJob
{
    private readonly ICollectionOptimizer _optimizer;
    private readonly ICobryxDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CollectionsOptimizerJob> _logger;

    public CollectionsOptimizerJob(
        ICollectionOptimizer optimizer,
        ICobryxDbContext dbContext,
        IConnectionMultiplexer redis,
        ILogger<CollectionsOptimizerJob> logger)
    {
        _optimizer = optimizer;
        _dbContext = dbContext;
        _redis = redis;
        _logger = logger;
    }

    public async Task RunHourlyOptimizationAsync()
    {
        _logger.LogInformation("Starting ML Optimizer Job...");
        var activeTenants = await _dbContext.CollectionOutcomes
            .Select(o => o.TenantId)
            .Distinct()
            .ToListAsync();

        var db = _redis.GetDatabase();
        var lockKey = "portfolio:collections:optimizer:lock";
        var token = System.Guid.NewGuid().ToString();

        // Idempotency Lock
        var acquired = await db.LockTakeAsync(lockKey, token, System.TimeSpan.FromMinutes(5));
        if (!acquired)
        {
            _logger.LogInformation("Optimizer is already running on another instance, skipping.");
            return;
        }

        try
        {
            foreach (var tenantId in activeTenants)
            {
                try
                {
                    var weights = await _optimizer.CalculateWeightsAsync(tenantId);
                    var key = $"portfolio:collections:weights:{tenantId}";
                    var json = System.Text.Json.JsonSerializer.Serialize(weights);

                    var tran = db.CreateTransaction();
                    _ = tran.KeyDeleteAsync(key);
                    _ = tran.StringSetAsync(key, json, System.TimeSpan.FromDays(1));
                    await tran.ExecuteAsync();
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, $"Optimization failed for tenant {tenantId}");
                }
            }
        }
        finally
        {
            await db.LockReleaseAsync(lockKey, token);
        }
        _logger.LogInformation("ML Optimizer Job completed.");
    }
}
