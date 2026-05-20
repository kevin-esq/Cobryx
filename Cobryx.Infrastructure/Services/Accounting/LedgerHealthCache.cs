using System.Collections.Concurrent;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Cobryx.Infrastructure.Services.Accounting;

/// <summary>
/// Infrastructure implementation of ILedgerHealthCache using IMemoryCache and thread-safe locking.
/// Optimized for O(1) synchronous lookups in the request pipeline.
/// </summary>
public class LedgerHealthCache(IMemoryCache cache) : ILedgerHealthCache
{
    private static readonly ConcurrentDictionary<Guid, object> _locks = new();
    private const string CacheKeyPrefix = "ledger_health:";

    public LedgerHealthStatus Get(Guid tenantId)
    {
        var key = BuildKey(tenantId);
        if (cache.TryGetValue(key, out LedgerHealthStatus? status) && status != null)
        {
            // Active expiration check
            if (status.ExpiresAt.HasValue && status.ExpiresAt.Value < DateTime.UtcNow)
            {
                Reset(tenantId);
                return CreateHealthyStatus();
            }

            return status;
        }

        return CreateHealthyStatus();
    }

    public bool TryActivateSafeMode(Guid tenantId, string reason, TimeSpan ttl)
    {
        var tenantLock = _locks.GetOrAdd(tenantId, _ => new object());

        lock (tenantLock)
        {
            var key = BuildKey(tenantId);
            
            // Check if already in safe mode
            if (cache.TryGetValue(key, out LedgerHealthStatus? current) && current?.IsSafeMode == true)
            {
                // Active expiration check
                if (current.ExpiresAt.HasValue && current.ExpiresAt.Value < DateTime.UtcNow)
                {
                    // If it was expired, we reset and allow new activation
                    Reset(tenantId);
                }
                else
                {
                    return false;
                }
            }

            var status = new LedgerHealthStatus(
                IsSafeMode: true,
                Reason: reason,
                TriggeredAt: DateTime.UtcNow,
                ExpiresAt: DateTime.UtcNow.Add(ttl));

            cache.Set(key, status, ttl);
            return true;
        }
    }

    public void Reset(Guid tenantId)
    {
        var key = BuildKey(tenantId);
        cache.Remove(key);
    }

    private static string BuildKey(Guid tenantId) => $"{CacheKeyPrefix}{tenantId}";

    private static LedgerHealthStatus CreateHealthyStatus() => new(false, "HEALTHY", DateTime.UtcNow, null);
}
