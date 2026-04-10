using Cobryx.Domain.ML;

namespace Cobryx.Application.ML.Interfaces;

public interface IFeatureStore
{
    public Task<FeatureVector> GetAsync(Guid customerId);
    public Task SetAsync(Guid customerId, FeatureVector features);
}
