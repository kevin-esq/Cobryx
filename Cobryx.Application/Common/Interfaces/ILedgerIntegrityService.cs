using Cobryx.Domain.Entities.Accounting;

namespace Cobryx.Application.Common.Interfaces;

public interface ILedgerIntegrityService
{
    /// <summary>
    /// Performs a journal replay. By default uses checkpoints for incremental speed,
    /// but can be forced to do a full historical audit.
    /// </summary>
    Task<IntegrityReport> VerifyJournalIntegrityAsync(Guid tenantId, bool forceFullReplay = false, CancellationToken ct = default);

    /// <summary>
    /// Checks if any financial circuit breakers should be tripped based on recent drifts.
    /// </summary>
    Task<bool> CheckCircuitBreakersAsync(Guid tenantId, CancellationToken ct = default);
}

public record IntegrityReport(
    bool IsHealthy,
    int TotalEntriesScanned,
    int ImbalancedTransactionsCount,
    int OrphanEntriesCount,
    string JournalFingerprint,
    List<string> CorruptionDetails,
    bool CircuitBreakerTripped);
