using Cobryx.Domain.Config;

namespace Cobryx.Application.ML;

public class ModelRouter
{
    public bool ShouldRunShadow()
    {
        return Random.Shared.NextDouble() < ExperimentConfig.ShadowTrafficPercentage;
    }

    public bool UsePpo()
    {
        return Random.Shared.NextDouble() < ExperimentConfig.PpoTrafficPercentage;
    }
}
