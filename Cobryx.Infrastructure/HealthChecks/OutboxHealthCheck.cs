using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cobryx.Infrastructure.HealthChecks;

public class OutboxHealthCheck : IHealthCheck
{
    private readonly CobryxDbContext _dbContext;
    private const int WarningThresholdMinutes = 1;
    private const int UnhealthyThresholdMinutes = 5;

    public OutboxHealthCheck(CobryxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var oldestUnprocessedEvent = await _dbContext.OutboxEvents
                .Where(e => e.ProcessedOnUtc == null)
                .OrderBy(e => e.OccurredOnUtc)
                .FirstOrDefaultAsync(ct);

            if (oldestUnprocessedEvent == null)
            {
                return HealthCheckResult.Healthy("Outbox queue is empty.");
            }

            var lag = DateTime.UtcNow - oldestUnprocessedEvent.OccurredOnUtc;
            var lagMinutes = lag.TotalMinutes;

            var data = new Dictionary<string, object>
            {
                { "OldestUnprocessedEventId", oldestUnprocessedEvent.Id },
                { "LagMinutes", Math.Round(lagMinutes, 2) },
                { "TotalUnprocessedCount", await _dbContext.OutboxEvents.CountAsync(e => e.ProcessedOnUtc == null, ct) }
            };

            if (lagMinutes > UnhealthyThresholdMinutes)
            {
                return HealthCheckResult.Unhealthy($"Outbox processing lag is critical: {Math.Round(lagMinutes, 2)} minutes.", data: data);
            }

            if (lagMinutes > WarningThresholdMinutes)
            {
                return HealthCheckResult.Degraded($"Outbox processing lag is high: {Math.Round(lagMinutes, 2)} minutes.", data: data);
            }

            return HealthCheckResult.Healthy($"Outbox processing lag: {Math.Round(lagMinutes, 2)} minutes.", data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Outbox health check failed: {ex.Message}");
        }
    }
}
