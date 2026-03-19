#pragma warning disable IDE0005
using System.Threading.Tasks;
using Cobryx.Application.Common.Interfaces;

namespace Cobryx.Application.ML;

public class RedisPortfolioFeatureStore : IPortfolioFeatureStore
{
    private readonly ICacheService _cache;

    public RedisPortfolioFeatureStore(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<Cobryx.Domain.ML.PortfolioState> GetGlobalStateAsync()
    {
        var state = await _cache.GetAsync<Cobryx.Domain.ML.PortfolioState>("portfolio:state", default);
        return state ?? new Cobryx.Domain.ML.PortfolioState 
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
