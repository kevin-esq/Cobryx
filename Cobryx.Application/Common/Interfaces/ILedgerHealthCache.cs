using Cobryx.Domain.Accounting.Models;

namespace Cobryx.Application.Common.Interfaces;

/// <summary>
/// High-performance, O(1) cache for tenant financial safety status.
/// Used by middleware to block operations without database round-trips.
/// </summary>
public interface ILedgerHealthCache
{
    /// <summary>
    /// Gets the current health status of a tenant.
    /// </summary>
    public LedgerHealthStatus Get(Guid tenantId);

    /// <summary>
    /// Idempotently attempts to activate safe mode for a tenant.
    /// Returns true if safe mode was newly activated, false if it was already active.
    /// </summary>
    public bool TryActivateSafeMode(Guid tenantId, string reason, TimeSpan ttl);

    /// <summary>
    /// Resets the safe mode for a tenant (SRE Manual Override).
    /// </summary>
    public void Reset(Guid tenantId);
}
