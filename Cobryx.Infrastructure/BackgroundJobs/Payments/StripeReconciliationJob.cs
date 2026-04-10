using System.Collections.Concurrent;

using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs.Payments;

/// <summary>
/// Automated Stripe reconciliation job that runs every 5 minutes.
/// Detects and auto-heals payment discrepancies between Stripe and the local database.
/// This is the final safety net for payment consistency.
///
/// Features:
/// - Tenant isolation: each tenant processed independently
/// - Parallelism control: max 4 concurrent tenants to prevent resource exhaustion
/// - Backpressure: skips if previous run still active
/// - Per-tenant metrics: isolated drift tracking
/// </summary>
public partial class StripeReconciliationJob(
    ICobryxDbContext dbContext,
    ReconciliationEngine reconciliationEngine,
    IProcessedWebhookEventRepository processedEventRepository,
    CobryxMetrics metrics,
    IClock clock,
    ILogger<StripeReconciliationJob> logger)
{
    private const int LookbackMinutes = 20;
    private const int OverlapMinutes = 5;
    private const int MaxDriftRatePercent = 10;
    private const int CriticalDriftThreshold = 5;
    private const int MaxParallelTenants = 4;
    private const int TenantTimeoutSeconds = 60;

    private static int _isRunning;

    public async Task RunAsync(CancellationToken ct = default)
    {
        // BACKPRESSURE: Skip if previous run still active
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
        {
            LogReconciliationSkippedBackpressure(logger);
            metrics.CobryxPressureBackoffActiveTotal.Add(1,
                new KeyValuePair<string, object?>("job", "stripe-reconciliation"));
            return;
        }

        try
        {
            await RunInternalAsync(ct);
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, 0);
        }
    }

    private async Task RunInternalAsync(CancellationToken ct)
    {
        LogReconciliationJobStarted(logger);

        DateTime to = clock.UtcNow;
        DateTime from = to.AddMinutes(-(LookbackMinutes + OverlapMinutes));

        var tenantsWithStripe = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.StripeAccountId != null && !t.IsPaymentRestricted)
            .Select(t => new { t.Id, t.StripeAccountId })
            .ToListAsync(ct);

        // TENANT ISOLATION: Process tenants in parallel with controlled concurrency
        var results = new ConcurrentBag<TenantReconciliationResult>();
        var semaphore = new SemaphoreSlim(MaxParallelTenants);

        var tasks = tenantsWithStripe.Select(async tenant =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                using var tenantCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                tenantCts.CancelAfter(TimeSpan.FromSeconds(TenantTimeoutSeconds));

                var result = await ProcessTenantWithIsolationAsync(
                    tenant.Id, tenant.StripeAccountId!, from, to, tenantCts.Token);
                results.Add(result);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                LogTenantTimeout(logger, tenant.Id);
                results.Add(new TenantReconciliationResult(tenant.Id, 0, 0, true, "Timeout"));
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        // Aggregate results
        var totalChecked = results.Count(r => !r.Failed);
        var totalMismatches = results.Sum(r => r.Mismatches);
        var totalAutoFixed = results.Sum(r => r.AutoFixed);
        var totalFailed = results.Count(r => r.Failed);

        LogReconciliationJobCompleted(logger, totalChecked, totalMismatches, totalAutoFixed, totalFailed);

        if (totalChecked > 0)
        {
            var driftRate = (totalMismatches * 100) / totalChecked;
            if (driftRate > MaxDriftRatePercent)
            {
                LogHighDriftRate(logger, driftRate);
                metrics.ReconciliationHighDriftRateAlert.Add(1);
            }
        }
    }

    private async Task<TenantReconciliationResult> ProcessTenantWithIsolationAsync(
        Guid tenantId,
        string stripeAccountId,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        try
        {
            var audit = await ReconcileTenantAsync(tenantId, stripeAccountId, from, to, ct);

            // PER-TENANT METRICS
            metrics.ReconciliationCheckedTotal.Add(1,
                new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));

            if (audit.DetectedDriftsCount > 0)
            {
                metrics.ReconciliationMismatchTotal.Add(audit.DetectedDriftsCount,
                    new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()),
                    new KeyValuePair<string, object?>("severity", audit.Severity.ToString()));
            }

            var autoFixed = audit.Status == ReconciliationStatus.Repaired ? 1 : 0;
            if (autoFixed > 0)
            {
                metrics.ReconciliationAutoFixedTotal.Add(1,
                    new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
            }

            if (audit.Severity >= ReconciliationSeverity.Error)
            {
                metrics.ReconciliationFailedTotal.Add(1,
                    new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()),
                    new KeyValuePair<string, object?>("status", audit.Status.ToString()));

                await TriggerAlertIfNeededAsync(tenantId, audit, ct);
            }

            return new TenantReconciliationResult(tenantId, audit.DetectedDriftsCount, autoFixed, false, null);
        }
        catch (Exception ex)
        {
            LogTenantReconciliationFailed(logger, ex, tenantId);
            metrics.ReconciliationFailedTotal.Add(1,
                new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()),
                new KeyValuePair<string, object?>("status", "Exception"));

            return new TenantReconciliationResult(tenantId, 0, 0, true, ex.Message);
        }
    }

    private record TenantReconciliationResult(
        Guid TenantId,
        int Mismatches,
        int AutoFixed,
        bool Failed,
        string? Error);

    private async Task<ReconciliationAudit> ReconcileTenantAsync(
        Guid tenantId,
        string stripeAccountId,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        var reconciliationKey = $"reconciliation:{tenantId}:{from:yyyyMMddHHmm}";

        var alreadyProcessed = await processedEventRepository.ExistsAsync(
            "Reconciliation", reconciliationKey, ct);

        if (alreadyProcessed)
        {
            LogReconciliationSkipped(logger, tenantId, "already processed");
            return new ReconciliationAudit(
                tenantId, Guid.NewGuid(), from, to,
                0, 0, 0,
                ReconciliationStatus.Synced, ReconciliationSeverity.Info,
                0, "[]");
        }

        var audit = await reconciliationEngine.ReconcileAsync(tenantId, from, to, stripeAccountId, ct);

        await processedEventRepository.TryMarkAsProcessedAsync(
            "Reconciliation",
            reconciliationKey,
            "scheduled_reconciliation",
            relatedEntityId: tenantId,
            relatedEntityType: "Tenant",
            ct: ct);

        return audit;
    }

    private async Task TriggerAlertIfNeededAsync(Guid tenantId, ReconciliationAudit audit, CancellationToken _)
    {
        if (audit.DetectedDriftsCount >= CriticalDriftThreshold ||
            audit.Severity == ReconciliationSeverity.Critical)
        {
            LogCriticalDriftAlert(logger, tenantId, audit.DetectedDriftsCount, audit.Severity);
            metrics.ReconciliationCriticalAlert.Add(1,
                new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));

            // TODO: Integrate with alerting system (Slack, PagerDuty, etc.)
            // await _alertService.SendCriticalAlertAsync(
            //     $"Critical payment drift detected for tenant {tenantId}",
            //     $"Detected {audit.DetectedDriftsCount} drifts with severity {audit.Severity}",
            //     ct);
        }

        await Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe reconciliation job started")]
    private static partial void LogReconciliationJobStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Stripe reconciliation job completed. Checked: {Checked}, Mismatches: {Mismatches}, AutoFixed: {AutoFixed}, Failed: {Failed}")]
    private static partial void LogReconciliationJobCompleted(ILogger logger, int @checked, int mismatches, int autoFixed, int failed);

    [LoggerMessage(Level = LogLevel.Error, Message = "Reconciliation failed for tenant {TenantId}")]
    private static partial void LogTenantReconciliationFailed(ILogger logger, Exception ex, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Reconciliation skipped for tenant {TenantId}: {Reason}")]
    private static partial void LogReconciliationSkipped(ILogger logger, Guid tenantId, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "High drift rate detected: {DriftRate}%")]
    private static partial void LogHighDriftRate(ILogger logger, int driftRate);

    [LoggerMessage(Level = LogLevel.Critical,
        Message = "CRITICAL: Payment drift alert for tenant {TenantId}. Drifts: {DriftCount}, Severity: {Severity}")]
    private static partial void LogCriticalDriftAlert(ILogger logger, Guid tenantId, int driftCount, ReconciliationSeverity severity);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Reconciliation skipped due to backpressure (previous run still active)")]
    private static partial void LogReconciliationSkippedBackpressure(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tenant {TenantId} reconciliation timed out")]
    private static partial void LogTenantTimeout(ILogger logger, Guid tenantId);
}
