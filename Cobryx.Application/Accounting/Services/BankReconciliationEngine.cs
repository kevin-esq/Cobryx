using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services;

public interface IBankReconciliationEngine
{
    public Task<BankReconciliationReport> ReconcileBankMovementsAsync(Guid tenantId, CancellationToken ct = default);
}

public class BankReconciliationEngine(
    ICobryxDbContext dbContext,
    ILedgerIntegrityService integrityService,
    IDatabaseDiagnosticService diagnosticService,
    CobryxMetrics metrics,
    ILogger<BankReconciliationEngine> logger) : IBankReconciliationEngine
{
    private readonly ICobryxDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ILedgerIntegrityService _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
    private readonly IDatabaseDiagnosticService _diagnosticService = diagnosticService ?? throw new ArgumentNullException(nameof(diagnosticService));
    private readonly CobryxMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    private readonly ILogger<BankReconciliationEngine> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<BankReconciliationReport> ReconcileBankMovementsAsync(Guid tenantId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Institutional Bank Reconciliation for Tenant {TenantId}", tenantId);

        var unmatchedMovements = await _dbContext.BankMovements
            .Where(m => m.TenantId == tenantId && m.Status == BankMovementStatus.Unmatched)
            .OrderBy(m => m.BookingDate)
            .ToListAsync(ct);

        var ledgerTransactions = await _dbContext.LedgerTransactions
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.IsPosted)
            .Include(t => t.Entries)
            .ToListAsync(ct);

        var report = new BankReconciliationReport();
        var totalJitterAppliedMs = 0;

        foreach (var movement in unmatchedMovements)
        {
            // Level 0: Pressure-Aware Backoff (Stay outside of DB transactions)
            totalJitterAppliedMs += await ApplyPressureBackoffAsync(totalJitterAppliedMs, ct);
            // Level 1: Exact Match (Ref + Amount)
            if (!string.IsNullOrEmpty(movement.ExternalRef))
            {
                var exactMatch = ledgerTransactions.FirstOrDefault(t =>
                    t.ReferenceId == movement.ExternalRef &&
                    t.Entries.Any(e => Math.Abs(e.Debit - e.Credit) == movement.Amount));

                if (exactMatch != null)
                {
                    movement.MarkAsMatched(exactMatch.Id, 1.0m, "Exact (Ref + Amount)");
                    report.AddMatchedResult(movement.Id, exactMatch.Id, 1.0m);
                    continue;
                }
            }

            // Level 2: Strong Match (Amount + Date Window ± 1 day)
            var strongMatches = ledgerTransactions.Where(t =>
                t.Entries.Any(e => Math.Abs(e.Debit - e.Credit) == movement.Amount) &&
                Math.Abs((t.EffectiveDate - movement.BookingDate).TotalDays) <= 1.5)
                .ToList();

            if (strongMatches.Count == 1)
            {
                var match = strongMatches.First();
                movement.MarkAsMatched(match.Id, 0.9m, "Strong (Amount + Date Window)");
                report.AddMatchedResult(movement.Id, match.Id, 0.9m);
                continue;
            }
            else if (strongMatches.Count > 1)
            {
                _logger.LogWarning("Ambiguous Bank Movement {MovementId}: Found {Count} potential Strong matches. Jumping to Investigation.", movement.Id, strongMatches.Count);
                movement.MarkForInvestigation();
                report.AddObservation($"Ambiguity for Movement {movement.Id}: Too many amount/date matches.");
                continue;
            }

            // Level 3: Aggregate Match (1:N settlements)
            var candidates = ledgerTransactions
                .Where(t => !report.MatchedItems.Any(m => m.LedgerTransactionId == t.Id))
                .Where(t => Math.Abs((t.EffectiveDate - movement.BookingDate).TotalDays) <= 3.0)
                .ToList();

            if (!string.IsNullOrEmpty(movement.ExternalRef))
            {
                var batchMatches = candidates.Where(t => t.ReferenceId == movement.ExternalRef).ToList();

                // Sum only the side that matches movement direction (Debits for Inbound, Credits for Outbound)
                decimal totalSideAmount = movement.Direction == BankMovementDirection.Inbound
                    ? batchMatches.SelectMany(static t => t.Entries).Sum(static e => Math.Max(0, e.Debit - e.Credit))
                    : batchMatches.SelectMany(static t => t.Entries).Sum(static e => Math.Max(0, e.Credit - e.Debit));

                if (batchMatches.Count > 1 && totalSideAmount == movement.Amount)
                {
                    // Heuristic: Logarithmic Decay
                    // confidence = 0.85 - (dateDrift * 0.03) - (log10(batchCount) * 0.05)
                    var maxDateDrift = (decimal)batchMatches.Max(t => Math.Abs((t.EffectiveDate - movement.BookingDate).TotalDays));
                    var confidence = 0.85m - (maxDateDrift * 0.03m) - ((decimal)Math.Log10(batchMatches.Count) * 0.05m);
                    confidence = Math.Clamp(confidence, 0.1m, 0.84m);

                    foreach (var match in batchMatches)
                    {
                        movement.MarkAsMatched(match.Id, confidence, "Aggregate (Batch Ref Log-Decay)");
                        report.AddMatchedResult(movement.Id, match.Id, confidence);
                    }
                    continue;
                }
            }

            report.UnmatchedCount++;
        }

        // Final Seal: Capture Ledger Fingerprint for SOC2 Audit
        var integrityReport = await _integrityService.VerifyJournalIntegrityAsync(tenantId, ct: ct);

        var audit = new ReconciliationAudit(
            tenantId,
            Guid.NewGuid(),
            unmatchedMovements.FirstOrDefault()?.BookingDate ?? DateTime.UtcNow,
            unmatchedMovements.LastOrDefault()?.BookingDate ?? DateTime.UtcNow,
            0, 0, 0, // Balances not tracked in this engine pass
            integrityReport.IsHealthy ? ReconciliationStatus.Synced : ReconciliationStatus.HardDrift,
            integrityReport.IsHealthy ? ReconciliationSeverity.Info : ReconciliationSeverity.Critical,
            report.UnmatchedCount,
            detailsJson: null,
            fingerprint: integrityReport.JournalFingerprint,
            lastCursor: null);

        _dbContext.ReconciliationAudits.Add(audit);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Bank Reconciliation completed and Sealed. Fingerprint: {Fingerprint}", integrityReport.JournalFingerprint);

        return report;
    }

    private async Task<int> ApplyPressureBackoffAsync(int currentTotalJitterMs, CancellationToken ct)
    {
        const int HardCapMs = 10_000; // SRE SLA Guard
        if (currentTotalJitterMs >= HardCapMs)
            return 0;

        var wraparoundRisk = await _diagnosticService.GetWraparoundRiskRatioAsync(ct);
        var deadTupleRatio = await _diagnosticService.GetLedgerDeadTupleRatioAsync(ct);

        // Thresholds: Risk > 0.70 or Dead Tuples > 20%
        if (wraparoundRisk > 0.70 || deadTupleRatio > 0.20)
        {
            var jitter = Random.Shared.Next(100, 501);

            _logger.LogWarning("System under Pressure (XID: {Risk:P}, DeadTuples: {Tuples:P}). Applying {Jitter}ms backoff.",
                wraparoundRisk, deadTupleRatio, jitter);

            _metrics.CobryxPressureBackoffActiveTotal.Add(1);

            await Task.Delay(jitter, ct);
            return jitter;
        }

        return 0;
    }
}

public class BankReconciliationReport
{
    public List<MatchedItem> MatchedItems { get; } = [];
    public int UnmatchedCount { get; set; }
    public List<string> Observations { get; } = [];

    public void AddMatchedResult(Guid movementId, Guid transactionId, decimal confidence)
    {
        MatchedItems.Add(new MatchedItem(movementId, transactionId, confidence));
    }

    public void AddObservation(string obs) => Observations.Add(obs);
}

public record MatchedItem(Guid BankMovementId, Guid LedgerTransactionId, decimal Confidence);
