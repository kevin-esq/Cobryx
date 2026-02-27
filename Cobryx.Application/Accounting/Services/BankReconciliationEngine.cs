using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Entities.Accounting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services;

public interface IBankReconciliationEngine
{
    Task<BankReconciliationReport> ReconcileBankMovementsAsync(Guid tenantId, CancellationToken ct = default);
}

public class BankReconciliationEngine(
    ICobryxDbContext dbContext,
    CobryxMetrics metrics,
    ILogger<BankReconciliationEngine> logger) : IBankReconciliationEngine
{
    private readonly ICobryxDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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

        foreach (var movement in unmatchedMovements)
        {
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
            // Look for unmatched transactions that share a ReferenceId or fall in the same window and sum to the movement amount
            var candidates = ledgerTransactions
                .Where(t => !report.MatchedItems.Any(m => m.LedgerTransactionId == t.Id))
                .Where(t => Math.Abs((t.EffectiveDate - movement.BookingDate).TotalDays) <= 1.5)
                .ToList();

            // Simplified Aggregate: Look for transactions with matching ReferenceId prefix (common for batches)
            if (!string.IsNullOrEmpty(movement.ExternalRef))
            {
                var batchMatches = candidates.Where(t => t.ReferenceId == movement.ExternalRef).ToList();
                if (batchMatches.Count > 1 && batchMatches.Sum(t => t.Entries.Sum(e => Math.Abs(e.Debit - e.Credit))) == movement.Amount)
                {
                    foreach (var match in batchMatches)
                    {
                        movement.MarkAsMatched(match.Id, 0.8m, "Aggregate (Batch Ref Match)");
                        report.AddMatchedResult(movement.Id, match.Id, 0.8m);
                    }
                    continue;
                }
            }

            report.UnmatchedCount++;
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Bank Reconciliation completed. Matched: {Matched}, Unmatched: {Unmatched}",
            report.MatchedItems.Count, report.UnmatchedCount);

        return report;
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
