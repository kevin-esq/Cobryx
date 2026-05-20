namespace Cobryx.Domain.Accounting.Models;

/// <summary>
/// Status record for a tenant's financial safety state.
/// </summary>
/// <param name="IsSafeMode">Whether the tenant is in lockdown.</param>
/// <param name="Reason">The cause of the lockdown (e.g. IMBALANCE, DRIFT).</param>
/// <param name="TriggeredAt">Timestamp when safe mode was first activated.</param>
/// <param name="ExpiresAt">Optional expiration for auto-reset scenarios.</param>
public record LedgerHealthStatus(
    bool IsSafeMode,
    string Reason,
    DateTime TriggeredAt,
    DateTime? ExpiresAt);
