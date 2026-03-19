namespace Cobryx.Application.ML;

public class ModelRouter
{
    public bool ShouldRunShadow()
    {
        return Random.Shared.NextDouble() < 0.3; // 30% shadow traffic
    }

    public bool UsePpo()
    {
        return Random.Shared.NextDouble() < 0.05; // 5% PPO
    }
}
