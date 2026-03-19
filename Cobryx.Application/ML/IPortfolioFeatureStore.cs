namespace Cobryx.Application.ML;

public interface IPortfolioFeatureStore
{
    public Task<Domain.ML.PortfolioState> GetGlobalStateAsync();
}
