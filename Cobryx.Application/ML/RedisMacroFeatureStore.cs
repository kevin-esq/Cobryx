using Cobryx.Application.Common.Interfaces;

namespace Cobryx.Application.ML;

public class RedisMacroFeatureStore(ICacheService cache) : IMacroFeatureStore
{
    public async Task<Cobryx.Domain.ML.MacroState> GetAsync()
    {
        var state = await cache.GetAsync<Cobryx.Domain.ML.MacroState>("macro:state", CancellationToken.None);
        
        if (state == null)
        {
            return new Cobryx.Domain.ML.MacroState 
            { 
                InterestRate = 0.05m,
                Inflation = 0.03m,
                Regime = Cobryx.Domain.ML.MarketRegime.Normal,
                Country = "US"
            };
        }
        
        return state;
    }
}
