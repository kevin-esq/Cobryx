using Cobryx.Domain.ML;

namespace Cobryx.Application.ML.Interfaces;

public interface IMacroFeatureStore
{
    public Task<MacroState> GetAsync();
}
