using System.Net.Http.Json;


namespace Cobryx.Application.ML;

public class PpoClient
{
    private readonly HttpClient _http;

    public PpoClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<PpoResponse> DecideAsync(object features)
    {
        var res = await _http.PostAsJsonAsync("/rl/ppo/decide", features);
        res.EnsureSuccessStatusCode();

        return (await res.Content.ReadFromJsonAsync<PpoResponse>())!;
    }

    public async Task<CombinedPpoResponse> DecideCombinedAsync(object payload)
    {
        var res = await _http.PostAsJsonAsync("/rl/ppo/combined", payload);
        res.EnsureSuccessStatusCode();

        return (await res.Content.ReadFromJsonAsync<CombinedPpoResponse>())!;
    }
}

public class CombinedPpoResponse
{
    public Cobryx.Domain.ML.PortfolioAction Portfolio { get; set; } = new();
    public PpoResponse Local { get; set; } = new();
}

public class PpoResponse
{
    public decimal CreditMultiplier { get; set; }
    public decimal InterestDelta { get; set; }
    public decimal LogProb { get; set; }
    public decimal Value { get; set; }
}
