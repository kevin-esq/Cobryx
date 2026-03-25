using System.Net.Http.Json;

namespace Cobryx.Application.ML;

public interface IMlClient
{
    public Task<(decimal pd, string version)> PredictAsync(object features, string modelVersion);
}

public class MlClient : IMlClient
{
    private readonly HttpClient _http;

    public MlClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<(decimal pd, string version)> PredictAsync(object features, string modelVersion)
    {
        var response = await _http.PostAsJsonAsync($"/predict?model={modelVersion}", features);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MlResponse>();

        return (result!.ProbabilityOfDefault, result.ModelVersion);
    }
}

public class MlResponse
{
    public decimal ProbabilityOfDefault { get; set; }
    public string ModelVersion { get; set; } = "";
}
