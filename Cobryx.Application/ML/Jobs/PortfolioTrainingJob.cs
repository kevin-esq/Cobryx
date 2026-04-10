using Cobryx.Application.ML.Interfaces;

namespace Cobryx.Application.ML.Jobs;

public class PortfolioTrainingJob(
    IPortfolioFeatureStore portfolioStore,
    IMacroFeatureStore macroStore)
{

    public async Task RunAsync()
    {
        var globalState = await portfolioStore.GetGlobalStateAsync();
        var macroState = await macroStore.GetAsync();

        var totalRevenue = globalState.RevenueYTD;
        var totalDefaultsLoss = globalState.TotalExposure * globalState.DefaultRate;
        var capitalCost = globalState.TotalCapital * 0.05m;

        var utilizationRatio = globalState.TotalCapital > 0
            ? (globalState.TotalCapital - globalState.AvailableLiquidity) / globalState.TotalCapital
            : 0m;
        var liquidityPenalty = utilizationRatio > 0.8m ? (utilizationRatio - 0.8m) * 1000000m : 0m;

        var volatilityPenalty = globalState.AveragePd * 10000m;

        var macroPenalty = (macroState.Inflation * globalState.TotalExposure) +
                           (macroState.MarketVolatility * globalState.TotalExposure * 2m);

        _ = totalRevenue - totalDefaultsLoss - capitalCost - liquidityPenalty - volatilityPenalty -
                           macroPenalty;

    }
}
