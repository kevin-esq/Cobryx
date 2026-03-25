using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Decision;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cobryx.Infrastructure.HealthChecks;

public class ShadowHealthCheck(ICobryxDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);

        var totalExecutions = await db.DecisionSnapshots
            .CountAsync(s => s.CreatedAt >= oneHourAgo, cancellationToken);

        if (totalExecutions == 0)
            return HealthCheckResult.Healthy("No shadow executions in the last hour.");

        var criticalDrifts = await db.ShadowDriftEvents
            .CountAsync(e => e.CreatedAt >= oneHourAgo && e.OutputSeverity == DriftSeverity.Critical, cancellationToken);

        var traceMismatches = await db.ShadowDriftEvents
            .CountAsync(e => e.CreatedAt >= oneHourAgo && e.TraceSeverity == DriftSeverity.Critical, cancellationToken);

        var criticalRate = (double)criticalDrifts / totalExecutions;

        if (criticalRate > 0.001 || traceMismatches > 0)
            return HealthCheckResult.Unhealthy(
                $"Shadow drift critical. Rate: {criticalRate:P2}, trace mismatches: {traceMismatches}",
                data: new Dictionary<string, object>
                {
                    { "CriticalRate",     criticalRate    },
                    { "TraceMismatches",  traceMismatches },
                    { "TotalExecutions",  totalExecutions }
                });

        var significantDrifts = await db.ShadowDriftEvents
            .CountAsync(e => e.CreatedAt >= oneHourAgo && e.OutputSeverity == DriftSeverity.Significant, cancellationToken);

        if (significantDrifts > 5)
            return HealthCheckResult.Degraded($"High volume of significant drifts: {significantDrifts}");

        return HealthCheckResult.Healthy($"Shadow health normal. Drift rate: {criticalRate:P2}");
    }
}