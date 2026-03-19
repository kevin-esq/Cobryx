using System.Net.Http.Json;

namespace Cobryx.Application.ML;

public class PortfolioPpoClient
{
    private readonly HttpClient _http;

    public PortfolioPpoClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<Cobryx.Domain.ML.PortfolioAction> DecideAsync(object state)
    {
        var res = await _http.PostAsJsonAsync("/rl/ppo/portfolio", state);
        res.EnsureSuccessStatusCode();

        return (await res.Content.ReadFromJsonAsync<Cobryx.Domain.ML.PortfolioAction>())!;
    }
}
