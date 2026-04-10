using System.Net.Http.Json;

using Cobryx.Application.ML.Interfaces;

namespace Cobryx.Infrastructure.ML.Clients;

public class MlClient(HttpClient http) : IMlClient
{
    public async Task<(decimal pd, string version)> PredictAsync(object features, string modelVersion)
    {
        var response = await http.PostAsJsonAsync($"/predict?model={modelVersion}", features);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MlResponse>();
        return (result!.ProbabilityOfDefault, result.ModelVersion);
    }

    private class MlResponse
    {
        public decimal ProbabilityOfDefault { get; set; }
        public string ModelVersion { get; set; } = "";
    }
}
