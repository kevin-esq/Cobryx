using Cobryx.Domain.ML;

namespace Cobryx.Application.ML.Interfaces;

public interface IPortfolioFeatureStore
{
    public Task<PortfolioState> GetGlobalStateAsync();
}
