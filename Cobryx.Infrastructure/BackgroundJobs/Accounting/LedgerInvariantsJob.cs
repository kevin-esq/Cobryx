using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs.Accounting;

/// <summary>
/// Enforces ledger invariants that MUST always hold true.
/// This is the final safety net for financial data integrity.
/// 
/// Invariants checked:
/// 1. Double-entry: sum(debits) == sum(credits) per transaction
/// 2. Balance non-negative: cash accounts >= 0
/// 3. No orphan entries: all entries have valid transactions
/// 4. Temporal consistency: no future-dated transactions
/// </summary>
public partial class LedgerInvariantsJob(
    ICobryxDbContext dbContext,
    CobryxMetrics metrics,
    IClock clock,
    ILogger<LedgerInvariantsJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        LogInvariantsCheckStarted(logger);

        var violations = new List<InvariantViolation>();

        // Check 1: Double-entry invariant (debits == credits per transaction)
        var unbalancedTransactions = await CheckDoubleEntryInvariantAsync(ct);
        violations.AddRange(unbalancedTransactions);

        // Check 2: Negative balance invariant
        var negativeBalances = await CheckNegativeBalanceInvariantAsync(ct);
        violations.AddRange(negativeBalances);

        // Check 3: Orphan entries invariant
        var orphanEntries = await CheckOrphanEntriesInvariantAsync(ct);
        violations.AddRange(orphanEntries);

        // Check 4: Future-dated transactions
        var futureDated = await CheckFutureDatedInvariantAsync(ct);
        violations.AddRange(futureDated);

        // Report metrics
        foreach (var violation in violations)
        {
            metrics.LedgerInvariantViolationTotal.Add(1,
                new KeyValuePair<string, object?>("type", violation.Type.ToString()),
                new KeyValuePair<string, object?>("tenant_id", violation.TenantId.ToString()),
                new KeyValuePair<string, object?>("severity", violation.Severity.ToString()));

            if (violation.Severity == ReconciliationSeverity.Critical)
            {
                LogCriticalViolation(logger, violation.Type, violation.TenantId, violation.Message);
            }
        }

        LogInvariantsCheckCompleted(logger, violations.Count);
    }

    private async Task<List<InvariantViolation>> CheckDoubleEntryInvariantAsync(CancellationToken ct)
    {
        var violations = new List<InvariantViolation>();

        // Find transactions where debits != credits
        var unbalanced = await dbContext.LedgerTransactions
            .AsNoTracking()
            .Include(t => t.Entries)
            .Where(t => t.Entries.Sum(e => e.Debit) != t.Entries.Sum(e => e.Credit))
            .Select(t => new { t.Id, t.TenantId, t.ReferenceId, Debits = t.Entries.Sum(e => e.Debit), Credits = t.Entries.Sum(e => e.Credit) })
            .Take(100) // Limit to prevent memory issues
            .ToListAsync(ct);

        foreach (var tx in unbalanced)
        {
            violations.Add(new InvariantViolation(
                InvariantType.DoubleEntry,
                tx.TenantId,
                ReconciliationSeverity.Critical,
                $"CRITICAL: Transaction {tx.Id} violates double-entry! Debits={tx.Debits:C}, Credits={tx.Credits:C}, Ref={tx.ReferenceId}"));
        }

        return violations;
    }

    private async Task<List<InvariantViolation>> CheckNegativeBalanceInvariantAsync(CancellationToken ct)
    {
        var violations = new List<InvariantViolation>();

        // Find cash accounts (1xxx) with negative balance
        var negativeAccounts = await dbContext.LedgerAccounts
            .AsNoTracking()
            .Where(a => a.Code.StartsWith("1")) // Asset accounts
            .Select(a => new
            {
                a.Id,
                a.TenantId,
                a.Code,
                a.Name,
                Balance = dbContext.LedgerEntries
                    .Where(e => e.AccountId == a.Id)
                    .Sum(e => e.Debit - e.Credit)
            })
            .Where(a => a.Balance < 0)
            .Take(100)
            .ToListAsync(ct);

        foreach (var account in negativeAccounts)
        {
            violations.Add(new InvariantViolation(
                InvariantType.NegativeBalance,
                account.TenantId,
                ReconciliationSeverity.Error,
                $"Asset account {account.Code} ({account.Name}) has negative balance: {account.Balance:C}"));
        }

        return violations;
    }

    private async Task<List<InvariantViolation>> CheckOrphanEntriesInvariantAsync(CancellationToken ct)
    {
        var violations = new List<InvariantViolation>();

        // Find entries without valid transaction
        var orphanCount = await dbContext.LedgerEntries
            .AsNoTracking()
            .Where(e => !dbContext.LedgerTransactions.Any(t => t.Id == e.TransactionId))
            .CountAsync(ct);

        if (orphanCount > 0)
        {
            violations.Add(new InvariantViolation(
                InvariantType.OrphanEntries,
                Guid.Empty, // System-wide
                ReconciliationSeverity.Critical,
                $"CRITICAL: Found {orphanCount} orphan ledger entries without valid transactions!"));
        }

        return violations;
    }

    private async Task<List<InvariantViolation>> CheckFutureDatedInvariantAsync(CancellationToken ct)
    {
        var violations = new List<InvariantViolation>();
        var now = clock.UtcNow;

        // Find transactions dated in the future
        var futureTransactions = await dbContext.LedgerTransactions
            .AsNoTracking()
            .Where(t => t.CreatedAt > now.AddMinutes(5)) // 5 min tolerance for clock skew
            .Select(t => new { t.Id, t.TenantId, t.CreatedAt, t.ReferenceId })
            .Take(100)
            .ToListAsync(ct);

        foreach (var tx in futureTransactions)
        {
            violations.Add(new InvariantViolation(
                InvariantType.FutureDated,
                tx.TenantId,
                ReconciliationSeverity.Warning,
                $"Transaction {tx.Id} is future-dated: {tx.CreatedAt:O}, Ref={tx.ReferenceId}"));
        }

        return violations;
    }

    private record InvariantViolation(
        InvariantType Type,
        Guid TenantId,
        ReconciliationSeverity Severity,
        string Message);

    private enum InvariantType
    {
        DoubleEntry,
        NegativeBalance,
        OrphanEntries,
        FutureDated
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Ledger invariants check started")]
    private static partial void LogInvariantsCheckStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ledger invariants check completed. Violations: {ViolationCount}")]
    private static partial void LogInvariantsCheckCompleted(ILogger logger, int violationCount);

    [LoggerMessage(Level = LogLevel.Critical, Message = "CRITICAL INVARIANT VIOLATION: {Type} for tenant {TenantId}. {Message}")]
    private static partial void LogCriticalViolation(ILogger logger, InvariantType type, Guid tenantId, string message);
}
