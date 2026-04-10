using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.ML.Interfaces;
using Cobryx.Domain.ML;

namespace Cobryx.Infrastructure.ML.FeatureStores;

public class RedisMacroFeatureStore(ICacheService cache) : IMacroFeatureStore
{
    public async Task<MacroState> GetAsync()
    {
        var state = await cache.GetAsync<MacroState>("macro:state", CancellationToken.None);

        if (state == null)
        {
            return new MacroState
            {
                InterestRate = 0.05m,
                Inflation = 0.03m,
                Regime = MarketRegime.Normal,
                Country = "US"
            };
        }

        return state;
    }
}
