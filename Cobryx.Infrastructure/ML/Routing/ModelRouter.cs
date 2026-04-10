using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.ML.Interfaces;
using Cobryx.Domain.Config;

namespace Cobryx.Infrastructure.ML.Routing;

public class ModelRouter(IRandomProvider rng) : IModelRouter
{
    public bool ShouldRunShadow()
    {
        return rng.NextDouble() < ExperimentConfig.ShadowTrafficPercentage;
    }

    public bool UsePpo()
    {
        return rng.NextDouble() < ExperimentConfig.PpoTrafficPercentage;
    }
}
