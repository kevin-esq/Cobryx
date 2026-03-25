using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Config;

namespace Cobryx.Application.ML;

public class ModelRouter(IRandomProvider rng)
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
