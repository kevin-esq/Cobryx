using System.Net.Http.Json;

using Cobryx.Domain.ML;

namespace Cobryx.Application.ML;

public class MonteCarloResponse
{
    public List<decimal> CreditMultipliers { get; set; } = new();
    public List<decimal> InterestDeltas { get; set; } = new();
    public List<decimal> Values { get; set; } = new();
    public List<decimal> LogProbs { get; set; } = new();
}

public class MonteCarloPpoClient(HttpClient http)
{
    public async Task<MonteCarloResponse> EvaluateBatchAsync(
        object features,
        PortfolioState globalState,
        MacroState currentMacro,
        List<Scenario> scenarios)
    {
        var payload = new
        {
            features,
            global_state = globalState,
            macro = currentMacro,
            scenarios
        };

        var res = await http.PostAsJsonAsync("/rl/ppo/montecarlo", payload);
        res.EnsureSuccessStatusCode();

        return (await res.Content.ReadFromJsonAsync<MonteCarloResponse>())!;
    }
}
