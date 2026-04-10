using Cobryx.Domain.ML;

namespace Cobryx.Application.ML.Interfaces;

public interface IPortfolioPpoClient
{
    public Task<PortfolioAction> DecideAsync(object state);
}
