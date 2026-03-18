using System.Net.Http.Json;
using Cobryx.Domain.ML;

namespace Cobryx.Application.ML;

public class MlClient(HttpClient http)
{
    private readonly HttpClient _http = http;

    public async Task<decimal> PredictAsync(FeatureVector f)
    {
        var res = await _http.PostAsJsonAsync("/predict", f);
        var data = await res.Content.ReadFromJsonAsync<MlResponse>();

        return data?.ProbabilityOfDefault ?? 0m;
    }

    private class MlResponse
    {
        public decimal ProbabilityOfDefault { get; set; }
    }
}
