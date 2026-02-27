using Cobryx.Domain.Entities.Accounting;

namespace Cobryx.Application.Common.Interfaces;

public interface ILedgerIntegrityService
{
    /// <summary>
    /// Performs a full journal replay to verify consistency and immutability.
    /// </summary>
    Task<IntegrityReport> VerifyJournalIntegrityAsync(Guid tenantId, CancellationToken ct = default);

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
