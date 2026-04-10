using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;

namespace Cobryx.Infrastructure.Services;

public class CachedUsageMeteringService : IUsageMeteringService
{
    private readonly IUsageMeteringService _inner;
    private readonly ICacheService _cacheService;
    private readonly CobryxMetrics _metrics;

    public CachedUsageMeteringService(
        IUsageMeteringService inner,
        ICacheService cacheService,
        CobryxMetrics metrics)
    {
        _inner = inner;
        _cacheService = cacheService;
        _metrics = metrics;
    }

    public async Task<UsageSnapshot> GetUsageSnapshotAsync(Guid tenantId, CancellationToken ct = default)
    {
        string cacheKey = IUsageMeteringService.GetCacheKey(tenantId);

        var cached = await _cacheService.GetAsync<UsageSnapshot>(cacheKey, ct);
        if (cached != null)
        {
            _metrics.UsageCacheHits.Add(1, new KeyValuePair<string, object?>("TenantId", tenantId));
            return cached;
        }

        _metrics.UsageCacheMisses.Add(1, new KeyValuePair<string, object?>("TenantId", tenantId));
        var snapshot = await _inner.GetUsageSnapshotAsync(tenantId, ct);

        await _cacheService.SetAsync(cacheKey, snapshot, TimeSpan.FromMinutes(5), ct);

        return snapshot;
    }
}
