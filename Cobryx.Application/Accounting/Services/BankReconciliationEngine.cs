using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services
{
    public interface IBankReconciliationEngine
    {
        public Task<BankReconciliationReport>
            ReconcileBankMovementsAsync(Guid tenantId, CancellationToken ct = default);
    }

    public partial class BankReconciliationEngine(
        ICobryxDbContext dbContext,
        ILedgerIntegrityService integrityService,
        IDatabaseDiagnosticService diagnosticService,
        CobryxMetrics metrics,
        IClock clock,
        ILogger<BankReconciliationEngine> logger) : IBankReconciliationEngine
    {
        private const int HardJitterCapMs = 10_000;

        public async Task<BankReconciliationReport> ReconcileBankMovementsAsync(Guid tenantId,
            CancellationToken ct = default)
        {
            LogStartingReconciliation(logger, tenantId);

            List<BankMovement> unmatchedMovements = await dbContext.BankMovements
                .Where(m => m.TenantId == tenantId && m.Status == BankMovementStatus.Unmatched)
                .OrderBy(static m => m.BookingDate)
                .ToListAsync(ct);

            List<LedgerTransaction> ledgerTransactions = await dbContext.LedgerTransactions
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId && t.IsPosted)
                .Include(static t => t.Entries)
                .ToListAsync(ct);

            var report = new BankReconciliationReport();
            var usedTransactionIds = new HashSet<Guid>();
            var totalJitter = 0;

            foreach (BankMovement movement in unmatchedMovements)
            {
                totalJitter += await ApplyPressureBackoffAsync(totalJitter, ct);

                if (TryExactMatch(movement, ledgerTransactions, report, usedTransactionIds))
                {
                    continue;
                }

                if (TryStrongMatch(movement, ledgerTransactions, report, usedTransactionIds))
                {
                    continue;
                }

                if (TryAggregateMatch(movement, ledgerTransactions, report, usedTransactionIds))
                {
                    continue;
                }

                report.UnmatchedCount++;
            }

            IntegrityReport integrityReport = await integrityService.VerifyJournalIntegrityAsync(tenantId, ct: ct);

            var audit = new ReconciliationAudit(
                tenantId,
                Guid.NewGuid(),
                unmatchedMovements.FirstOrDefault()?.BookingDate ?? clock.UtcNow,
                unmatchedMovements.LastOrDefault()?.BookingDate ?? clock.UtcNow,
                0,
                0,
                0,
                integrityReport.IsHealthy ? ReconciliationStatus.Synced : ReconciliationStatus.HardDrift,
                integrityReport.IsHealthy ? ReconciliationSeverity.Info : ReconciliationSeverity.Critical,
                report.UnmatchedCount,
                detailsJson: null,
                fingerprint: integrityReport.JournalFingerprint,
                lastCursor: null);

            _ = dbContext.ReconciliationAudits.Add(audit);
            _ = await dbContext.SaveChangesAsync(ct);

            LogReconciliationCompleted(logger, integrityReport.JournalFingerprint);

            return report;
        }

        private static bool TryExactMatch(
            BankMovement movement,
            List<LedgerTransaction> transactions,
            BankReconciliationReport report,
            HashSet<Guid> used)
        {
            if (string.IsNullOrEmpty(movement.ExternalRef))
            {
                return false;
            }

            LedgerTransaction? match = transactions.FirstOrDefault(t =>
                !used.Contains(t.Id) &&
                t.ReferenceId == movement.ExternalRef &&
                t.Entries.Any(e => Math.Abs(e.Debit - e.Credit) == movement.Amount));

            if (match is null)
            {
                return false;
            }

            _ = used.Add(match.Id);
            movement.MarkAsMatched(match.Id, 1.0m, "Exact (Ref + Amount)");
            report.AddMatchedResult(movement.Id, match.Id, 1.0m);

            return true;
        }

        private bool TryStrongMatch(
            BankMovement movement,
            List<LedgerTransaction> transactions,
            BankReconciliationReport report,
            HashSet<Guid> used)
        {
            var matches = transactions
                .Where(t =>
                    !used.Contains(t.Id) &&
                    t.Entries.Any(e => Math.Abs(e.Debit - e.Credit) == movement.Amount) &&
                    Math.Abs((t.EffectiveDate - movement.BookingDate).TotalDays) <= 1.5)
                .ToList();

            switch (matches.Count)
            {
                case 1:
                    {
                        LedgerTransaction match = matches[0];
                        _ = used.Add(match.Id);

                        movement.MarkAsMatched(match.Id, 0.9m, "Strong (Amount + Date Window)");
                        report.AddMatchedResult(movement.Id, match.Id, 0.9m);

                        return true;
                    }
                case > 1:
                    LogAmbiguousMovement(logger, movement.Id, matches.Count);
                    movement.MarkForInvestigation();
                    report.AddObservation($"Ambiguity for Movement {movement.Id}: Too many amount/date matches.");
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryAggregateMatch(
            BankMovement movement,
            List<LedgerTransaction> transactions,
            BankReconciliationReport report,
            HashSet<Guid> used)
        {
            if (string.IsNullOrEmpty(movement.ExternalRef))
            {
                return false;
            }

            var candidates = transactions
                .Where(t =>
                    !used.Contains(t.Id) &&
                    Math.Abs((t.EffectiveDate - movement.BookingDate).TotalDays) <= 3.0)
                .ToList();

            var batch = candidates
                .Where(t => t.ReferenceId == movement.ExternalRef)
                .ToList();

            if (batch.Count <= 1)
            {
                return false;
            }

            var total = movement.Direction == BankMovementDirection.Inbound
                ? batch.SelectMany(static t => t.Entries).Sum(static e => Math.Max(0, e.Debit - e.Credit))
                : batch.SelectMany(static t => t.Entries).Sum(static e => Math.Max(0, e.Credit - e.Debit));

            if (total != movement.Amount)
            {
                return false;
            }

            var maxDrift = (decimal)batch.Max(t => Math.Abs((t.EffectiveDate - movement.BookingDate).TotalDays));
            var confidence = 0.85m - (maxDrift * 0.03m) - ((decimal)Math.Log10(batch.Count) * 0.05m);
            confidence = Math.Clamp(confidence, 0.1m, 0.84m);

            foreach (LedgerTransaction match in batch)
            {
                _ = used.Add(match.Id);
                movement.MarkAsMatched(match.Id, confidence, "Aggregate (Batch Ref Log-Decay)");
                report.AddMatchedResult(movement.Id, match.Id, confidence);
            }

            return true;
        }

        private async Task<int> ApplyPressureBackoffAsync(int currentTotalJitterMs, CancellationToken ct)
        {
            if (currentTotalJitterMs >= HardJitterCapMs)
            {
                return 0;
            }

            var wraparoundRisk = await diagnosticService.GetWraparoundRiskRatioAsync(ct);
            var deadTupleRatio = await diagnosticService.GetLedgerDeadTupleRatioAsync(ct);

            if (wraparoundRisk <= 0.70 && deadTupleRatio <= 0.20)
            {
                return 0;
            }

            var jitter = Random.Shared.Next(100, 501);

            LogSystemPressureBackoff(logger, wraparoundRisk, deadTupleRatio, jitter);

            metrics.CobryxPressureBackoffActiveTotal.Add(1);

            await Task.Delay(jitter, ct);

            return jitter;
        }
    }

    public sealed class BankReconciliationReport
    {
        public List<MatchedItem> MatchedItems { get; } = [];
        public int UnmatchedCount { get; set; }
        public List<string> Observations { get; } = [];

        public void AddMatchedResult(Guid movementId, Guid transactionId, decimal confidence)
            => MatchedItems.Add(new MatchedItem(movementId, transactionId, confidence));

        public void AddObservation(string observation)
            => Observations.Add(observation);
    }

    public sealed record MatchedItem(Guid BankMovementId, Guid LedgerTransactionId, decimal Confidence);
}
