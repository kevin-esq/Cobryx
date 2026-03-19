#pragma warning disable IDE0005
using System.Threading.Tasks;
using Cobryx.Domain.ML;

namespace Cobryx.Application.ML;

public class PortfolioEngine
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
