namespace Cobryx.Application.ML.Interfaces;

public interface IMlClient
{
    public Task<(decimal pd, string version)> PredictAsync(object features, string modelVersion);
}
