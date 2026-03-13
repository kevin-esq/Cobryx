namespace Cobryx.Application.Common.Interfaces;

public interface ILedgerIntegrityService
{
    /// <summary>
    /// Performs a journal replay. By default uses checkpoints for incremental speed,
    /// but can be forced to do a full historical audit.
    /// </summary>
    public Task<IntegrityReport> VerifyJournalIntegrityAsync(Guid tenantId, bool forceFullReplay = false, CancellationToken ct = default);

    /// <summary>
    /// Checks if any financial circuit breakers should be tripped based on recent drifts.
    /// </summary>
    public Task<bool> CheckCircuitBreakersAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Scans the global ledger for sequence gaps (missing entries).
    /// </summary>
    public Task<List<long>> VerifyGlobalSequenceGapsAsync(CancellationToken ct = default);

    /// <summary>
    /// Verifies the global zero-sum invariant across the entire ledger.
    /// </summary>
    public Task<bool> VerifyGlobalSumInvariantAsync(CancellationToken ct = default);

    /// <summary>
    /// Verifies a specific account snapshot against the current ledger delta.
    /// </summary>
    public Task<bool> VerifyAccountSnapshotAsync(Guid snapshotId, CancellationToken ct = default);
}

public record IntegrityReport(
    bool IsHealthy,
    int TotalEntriesScanned,
    int ImbalancedTransactionsCount,
    int OrphanEntriesCount,
    string JournalFingerprint,
    List<string> CorruptionDetails,
    bool CircuitBreakerTripped);
