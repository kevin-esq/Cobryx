using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cobryx.Application.Accounting.Services;

public record BalanceResult(decimal Balance, long JournalSequenceId);

public interface ILedgerBalanceService
{
    /// <summary>
    /// Gets the current balance for an account with O(1) Redis lookup or Snapshot+Delta fallback.
    /// </summary>
    Task<BalanceResult> GetBalanceAsync(Guid tenantId, Guid accountId, CancellationToken ct = default);

    /// <summary>
    /// Gets the historical balance for an account at a specific journal sequence ID.
    /// </summary>
    Task<BalanceResult> GetHistoricalBalanceAsync(Guid tenantId, Guid accountId, long journalSequenceId, CancellationToken ct = default);

    /// <summary>
    /// Updates the cached balance for an account based on a delta incrementally.
    /// Used by the CDC Outbox Worker after confirmed SQL commit.
    /// </summary>
    Task UpdateCacheAsync(Guid tenantId, Guid accountId, decimal balanceChange, long newSequenceId, CancellationToken ct = default);

    /// <summary>
    /// Manually rebuilds an account snapshot at the current sequence.
    /// </summary>
    Task RebuildAccountSnapshotAsync(Guid tenantId, Guid accountId, long sequenceId, CancellationToken ct = default);

    /// <summary>
    /// Verifies the balance integrity by comparing cache/snapshot against deep ledger sum.
    /// </summary>
    Task<bool> VerifyBalanceIntegrityAsync(Guid tenantId, Guid accountId, CancellationToken ct = default);

    /// <summary>
    /// Rebuilds the cache for all active accounts of a tenant.
    /// </summary>
    Task WarmupCacheAsync(Guid tenantId, CancellationToken ct = default);
}
