using System.Diagnostics;
using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Application.Decision.Interfaces;
using Cobryx.Application.Decision.Models;
using Cobryx.Domain.Decision;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Decision;

public class ShadowMonitor(
    ICobryxDbContext db,
    CobryxMetrics metrics,
    IAlertingService alerting,
    ILogger<ShadowMonitor> logger) : IShadowMonitor
{
    public async Task RecordAsync(ShadowExecutionResult result, DecisionContext context)
    {
        LogMetrics(result);

        if (result.IsCritical)
        {
            logger.LogWarning(
                "CRITICAL SHADOW DRIFT DETECTED: Snapshot {SnapshotId}. Primary: {PrimaryVersion}, Shadow: {ShadowVersion}. Severity: {Severity}",
                result.SnapshotId, result.Primary.ReplayedEngineVersion, result.Shadow.ReplayedEngineVersion,
                result.OutputSeverity);

            var driftEvent = new ShadowDriftEvent(
                result.SnapshotId,
                result.Primary.ReplayedEngineVersion,
                result.Shadow.ReplayedEngineVersion,
                result.DeltaLimit,
                result.DeltaRate,
                result.OutputSeverity,
                result.TraceSeverity,
                JsonSerializer.Serialize(result.Attribution));

            db.ShadowDriftEvents.Add(driftEvent);
            await db.SaveChangesAsync(CancellationToken.None);

            await alerting.SendAlertAsync(
                "ShadowMonitor",
                $"Critical Shadow Drift in {result.Shadow.ReplayedEngineVersion}: Snapshot {result.SnapshotId}",
                AlertLevel.Critical,
                result);
        }
        else if (result.OutputSeverity != DriftSeverity.None || result.TraceSeverity != DriftSeverity.None)
        {
            logger.LogInformation(
                "Minor/Significant Shadow Drift: Snapshot {SnapshotId}. DeltaLimit: {DeltaLimit}",
                result.SnapshotId, result.DeltaLimit);
        }
    }

    private void LogMetrics(ShadowExecutionResult result)
    {
        var version  = result.Shadow.ReplayedEngineVersion;
        var tenant   = result.SnapshotId.ToString();
        const string decision = "credit_limit";

        var tags = new TagList
        {
            { "engine_version",  version   },
            { "tenant_id",       tenant    },
            { "decision_type",   decision  }
        };

        metrics.ShadowExecutionTotal.Add(1, tags);

        if (result.OutputSeverity != DriftSeverity.None)
        {
            metrics.ShadowDriftedTotal.Add(1, new TagList
            {
                { "engine_version", version  },
                { "tenant_id",      tenant   },
                { "decision_type",  decision },
                { "severity",       result.OutputSeverity.ToString().ToLowerInvariant() }
            });
            metrics.ShadowComparisonDelta.Record((double)Math.Abs(result.DeltaLimit), tags);
        }

        if (result.OutputSeverity == DriftSeverity.Critical)
            metrics.ShadowCriticalDriftTotal.Add(1, tags);

        var traceTags = new TagList
        {
            { "engine_version", version  },
            { "decision_type",  decision }
        };

        if (result.TraceSeverity == DriftSeverity.Critical)
            metrics.ShadowTraceMismatchTotal.Add(1, traceTags);
        else if (result.TraceSeverity != DriftSeverity.None)
            metrics.ShadowNonComparableTotal.Add(1, traceTags);
    }
}