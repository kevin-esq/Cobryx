using System.Net.Http.Json;

using Cobryx.Application.ML.Interfaces;
using Cobryx.Domain.ML;

namespace Cobryx.Infrastructure.ML.Clients;

public class PortfolioPpoClient(HttpClient http) : IPortfolioPpoClient
{
    public async Task<PortfolioAction> DecideAsync(object state)
    {
        var res = await http.PostAsJsonAsync("/rl/ppo/portfolio", state);
        res.EnsureSuccessStatusCode();

        return (await res.Content.ReadFromJsonAsync<PortfolioAction>())!;
    }
}
