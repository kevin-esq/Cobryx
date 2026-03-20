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

public class PpoDecisionResponse
{
    public decimal CreditMultiplier { get; set; }
    public decimal InterestDelta { get; set; }
    public decimal LogProb { get; set; }
    public decimal Value { get; set; }
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

    public async Task<PpoDecisionResponse> DecideAsync(object state)
    {
        var res = await http.PostAsJsonAsync("/rl/ppo/decide", new { state });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<PpoDecisionResponse>())!;
    }

    public async Task TrainAsync(object batchPayload)
    {
        var res = await http.PostAsJsonAsync("/rl/ppo/train", batchPayload);
        res.EnsureSuccessStatusCode();
    }
}
