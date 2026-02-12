using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;

namespace Cobryx.Infrastructure.Services;

public class CachedUsageMeteringService : IUsageMeteringService
{
    private readonly IUsageMeteringService _inner;
    private readonly ICacheService _cacheService;
    private const string CacheKeyPrefix = "usage:snapshot:";

    public CachedUsageMeteringService(
        IUsageMeteringService inner,
        ICacheService cacheService)
    {
        _inner = inner;
        _cacheService = cacheService;
    }

    public async Task<UsageSnapshot> GetUsageSnapshotAsync(Guid tenantId, CancellationToken ct = default)
    {
        string cacheKey = IUsageMeteringService.GetCacheKey(tenantId);
        
        var cached = await _cacheService.GetAsync<UsageSnapshot>(cacheKey, ct);
        if (cached != null)
        {
            return cached;
        }

        var snapshot = await _inner.GetUsageSnapshotAsync(tenantId, ct);
        
        // Cache for 5 minutes by default
        await _cacheService.SetAsync(cacheKey, snapshot, TimeSpan.FromMinutes(5), ct);
        
        return snapshot;
    }
}
