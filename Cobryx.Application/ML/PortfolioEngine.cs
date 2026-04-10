using Cobryx.Application.ML.Interfaces;
using Cobryx.Domain.ML;

namespace Cobryx.Application.ML;

public interface IPortfolioEngine
{
    public Task<PortfolioAction> OptimizeAsync(PortfolioState state);
}

public class PortfolioEngine(IPortfolioPpoClient ppo) : IPortfolioEngine
{

    public async Task<PortfolioAction> OptimizeAsync(PortfolioState state)
    {
        return await ppo.DecideAsync(state);
    }
}
