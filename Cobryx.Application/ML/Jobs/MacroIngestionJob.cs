using Cobryx.Application.Common.Interfaces;

namespace Cobryx.Application.ML.Jobs;

public class MacroIngestionJob(ICacheService cache)
{
    public async Task RunAsync()
    {
        var newRate = await FetchRate();
        var newInflation = await FetchInflation();
        var newSpread = await FetchSpread();
        var newVolatility = await FetchVolatility();

        var prevMacro = await cache.GetAsync<Cobryx.Domain.ML.MacroState>("macro:state", CancellationToken.None)
                        ?? new Cobryx.Domain.ML.MacroState();

        var smoothedInflation = 0.7m * prevMacro.Inflation + 0.3m * newInflation;
        var smoothedRate = 0.7m * prevMacro.InterestRate + 0.3m * newRate;

        var regime = smoothedInflation > 0.08m ? Cobryx.Domain.ML.MarketRegime.HighInflation :
            newVolatility > 0.3m ? Cobryx.Domain.ML.MarketRegime.Crisis : Cobryx.Domain.ML.MarketRegime.Normal;

        var macro = new Cobryx.Domain.ML.MacroState
        {
            InterestRate = smoothedRate,
            Inflation = smoothedInflation,
            Unemployment = 0.04m,
            CreditSpread = newSpread,
            MarketVolatility = newVolatility,
            LiquidityIndex = 1.0m,
            Regime = regime,
            Country = "US",

            InflationTMinus1 = prevMacro.Inflation,
            InflationTMinus2 = prevMacro.InflationTMinus1,
            RateTrend = smoothedRate - prevMacro.InterestRate
        };

        await cache.SetAsync("macro:state", macro);
    }

    private Task<decimal> FetchRate() => Task.FromResult(0.055m);
    private Task<decimal> FetchInflation() => Task.FromResult(0.04m);
    private Task<decimal> FetchSpread() => Task.FromResult(0.02m);
    private Task<decimal> FetchVolatility() => Task.FromResult(0.15m);
}
