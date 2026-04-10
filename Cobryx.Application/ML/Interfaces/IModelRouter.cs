namespace Cobryx.Application.ML.Interfaces;

public interface IModelRouter
{
    public bool ShouldRunShadow();
    public bool UsePpo();
}
