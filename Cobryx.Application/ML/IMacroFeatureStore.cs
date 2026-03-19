namespace Cobryx.Application.ML;

public interface IMacroFeatureStore
{
    public Task<Cobryx.Domain.ML.MacroState> GetAsync();
}
