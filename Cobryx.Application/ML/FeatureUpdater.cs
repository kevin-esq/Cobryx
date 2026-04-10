using Cobryx.Application.ML.Interfaces;

namespace Cobryx.Application.ML;

public class FeatureUpdater(IFeatureStore store)
{
    public async Task UpdateFromPayment(Guid customerId, decimal delay)
    {
        var f = await store.GetAsync(customerId);
        f.PaymentDelay = delay;
        await store.SetAsync(customerId, f);
    }
}
