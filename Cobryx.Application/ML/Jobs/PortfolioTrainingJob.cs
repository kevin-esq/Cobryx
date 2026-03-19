#pragma warning disable IDE0005
using System.Threading.Tasks;
using Cobryx.Application.Common.Interfaces;

namespace Cobryx.Application.ML.Jobs;

public class PortfolioTrainingJob
{
    private readonly ICobryxDbContext _db;
    private readonly IPortfolioFeatureStore _portfolioStore;

    public PortfolioTrainingJob(ICobryxDbContext db, IPortfolioFeatureStore portfolioStore)
    {
        _db = db;
        _portfolioStore = portfolioStore;
    }

    public async Task RunAsync()
    {
        var globalState = await _portfolioStore.GetGlobalStateAsync();

        // 16F / 16.7: Liquidity Penalty in Global Reward Function
        var totalRevenue = globalState.RevenueYTD;
        var totalDefaultsLoss = globalState.TotalExposure * globalState.DefaultRate;
        var capitalCost = globalState.TotalCapital * 0.05m;
        
        // Liquidity Penalty: f(utilization_of_capital)
        var utilizationRatio = globalState.TotalCapital > 0 ? (globalState.TotalCapital - globalState.AvailableLiquidity) / globalState.TotalCapital : 0m;
        var liquidityPenalty = utilizationRatio > 0.8m ? (utilizationRatio - 0.8m) * 1000000m : 0m;
        
        var volatilityPenalty = globalState.AveragePd * 10000m;

        var globalReward = totalRevenue - totalDefaultsLoss - capitalCost - liquidityPenalty - volatilityPenalty;

        // In a real system, push globalReward and (globalState -> nextState) to the multi-agent PyTorch Replay Buffer.
        // For now, we simulate the Portfolio RL loop.
    }
}
