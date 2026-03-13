using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs.Accounting;

public class DriftDetectionWorker
{
    private readonly ICobryxDbContext _context;
    private readonly ILedgerBalanceService _balanceService;
    private readonly CobryxMetrics _metrics;
    private readonly ILogger<DriftDetectionWorker> _logger;

    public DriftDetectionWorker(
        ICobryxDbContext context,
        ILedgerBalanceService balanceService,
        CobryxMetrics metrics,
        ILogger<DriftDetectionWorker> logger)
    {
        _context = context;
        _balanceService = balanceService;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Shadow Drift Detection Pass...");

        // 1. Lag Monitoring & Alerting
        var latestLedgerSeq = await _context.LedgerEntries.MaxAsync(e => (long?)e.JournalSequenceId, ct) ?? 0L;
        var latestShadowSeq = await _context.ShadowBalances.MaxAsync(s => (long?)s.LastSequence, ct) ?? 0L;

        var lag = latestLedgerSeq - latestShadowSeq;
        _metrics.ShadowReplayLag.Record(lag);

        if (lag > 5000)
        {
            _logger.LogCritical("CRITICAL SRE LAG DETECTED: {Lag} sequences behind. Drift detection suspended.", lag);
            return;
        }

        if (lag > 2000)
        {
            _logger.LogWarning("SRE LAG WARNING: {Lag} sequences behind.", lag);
        }

        // 2. Pinpoint Drift Detection
        var shadowBalances = await _context.ShadowBalances
            .AsNoTracking()
            .ToListAsync(ct);

        foreach (var shadow in shadowBalances)
        {
            // Only compare if we have the exact snapshot sequence ready in the ledger
            // This is "Pinpoint Detection" (matching same sequence)
            var ledgerResult = await _balanceService.GetHistoricalBalanceAsync(shadow.TenantId, shadow.AccountId, shadow.LastSequence, ct);

            if (Math.Abs(ledgerResult.Balance - shadow.Balance) > 0.0001m)
            {
                _logger.LogCritical("DRIFT DETECTED: Tenant {TenantId}, Account {AccountId}. Shadow={Shadow}, Ledger={Ledger} @ Seq {Seq}",
                    shadow.TenantId, shadow.AccountId, shadow.Balance, ledgerResult.Balance, shadow.LastSequence);

                _metrics.ShadowDriftDetected.Add(1);
                await TryTripFinancialSafeModeAsync(shadow.TenantId, ct);
            }
        }

        _logger.LogInformation("Shadow Drift Detection Pass completed. Accounts checked: {Count}", shadowBalances.Count);
    }

    private async Task TryTripFinancialSafeModeAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant != null && !tenant.FinancialSafeMode)
        {
            _logger.LogCritical("TRIPPING FINANCIAL SAFE MODE for Tenant {TenantId} due to drift detection failure.", tenantId);
            tenant.ToggleFinancialSafeMode(true);
            await _context.SaveChangesAsync(ct);
            _metrics.CircuitBreakerTrippedTotal.Add(1, new KeyValuePair<string, object?>("tenant_id", tenantId.ToString()));
        }
    }
}
