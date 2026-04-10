using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.ML.Interfaces;
using Cobryx.Domain.ML;

namespace Cobryx.Infrastructure.ML.FeatureStores;

public class RedisFeatureStore(ICacheService cache) : IFeatureStore
{
    public async Task<FeatureVector> GetAsync(Guid customerId)
    {
        var result = await cache.GetAsync<FeatureVector>($"features:{customerId}");
        return result ?? new FeatureVector();
    }

    public Task SetAsync(Guid customerId, FeatureVector features)
    {
        return cache.SetAsync($"features:{customerId}", features, TimeSpan.FromHours(6));
    }
}
