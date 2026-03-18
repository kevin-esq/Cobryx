#pragma warning disable IDE0005
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Cobryx.Application.ML;

public class MlClient
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
