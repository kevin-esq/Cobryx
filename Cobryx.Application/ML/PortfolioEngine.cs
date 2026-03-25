using Cobryx.Domain.ML;

namespace Cobryx.Application.ML;

public interface IPortfolioEngine
{
    public Task<PortfolioAction> OptimizeAsync(PortfolioState state);
}

public class PortfolioEngine : IPortfolioEngine
{
    private readonly PortfolioPpoClient _ppo;

    public PortfolioEngine(PortfolioPpoClient ppo)
    {
        _ppo = ppo;
    }

    public async Task<PortfolioAction> OptimizeAsync(PortfolioState state)
    {
        return await _ppo.DecideAsync(state);
    }
}
