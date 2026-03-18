using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.ML;

namespace Cobryx.Application.ML;

public class RedisFeatureStore(ICacheService cache) : IFeatureStore
{
    private readonly ICacheService _cache = cache;

    public async Task<FeatureVector> GetAsync(Guid customerId)
    {
        var result = await _cache.GetAsync<FeatureVector>($"features:{customerId}");
        return result ?? new FeatureVector();
    }

    public Task SetAsync(Guid customerId, FeatureVector features)
    {
        return _cache.SetAsync($"features:{customerId}", features, TimeSpan.FromHours(6));
    }
}
