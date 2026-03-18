namespace Cobryx.Application.ML;

public class ModelRouter
{
    public bool ShouldRunShadow()
    {
        return Random.Shared.NextDouble() < 0.3; // 30% tráfico shadow
    }
}
