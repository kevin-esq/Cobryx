using System;
using System.Threading.Tasks;

namespace Cobryx.Application.ML;

public class FeatureUpdater
{
    private readonly IFeatureStore _store;

    public FeatureUpdater(IFeatureStore store)
    {
        _store = store;
    }

    public async Task UpdateFromPayment(Guid customerId, decimal delay)
    {
        var f = await _store.GetAsync(customerId);
        f.PaymentDelay = delay;
        await _store.SetAsync(customerId, f);
    }
}
