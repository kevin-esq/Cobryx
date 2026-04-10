using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.ML.Interfaces;
using Cobryx.Domain.ML;

namespace Cobryx.Infrastructure.ML.FeatureStores;

public class RedisPortfolioFeatureStore(ICacheService cache) : IPortfolioFeatureStore
{
    public async Task<PortfolioState> GetGlobalStateAsync()
    {
        PortfolioState? state = await cache.GetAsync<PortfolioState>("portfolio:state", CancellationToken.None);

        return state ?? new PortfolioState
        {
            TotalCapital = 1000000m,
            AvailableLiquidity = 500000m,
            TotalExposure = 500000m,
            AveragePd = 0.15m,
            DefaultRate = 0.05m,
            ActiveLoans = 1000,
            RevenueYTD = 50000m
        };
    }
}
