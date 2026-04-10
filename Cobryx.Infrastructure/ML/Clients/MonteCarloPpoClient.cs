using System.Net.Http.Json;

using Cobryx.Application.ML.Interfaces;
using Cobryx.Domain.ML;

namespace Cobryx.Infrastructure.ML.Clients;

public class MonteCarloPpoClient(HttpClient http) : IMonteCarloPpoClient
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

        var response = await res.Content.ReadFromJsonAsync<MonteCarloResponseDto>();
        return new MonteCarloResponse(
            response!.CreditMultipliers,
            response.InterestDeltas,
            response.Values,
            response.LogProbs);
    }

    public async Task<PpoDecisionResponse> DecideAsync(object state)
    {
        var res = await http.PostAsJsonAsync("/rl/ppo/decide", new { state });
        res.EnsureSuccessStatusCode();

        var response = await res.Content.ReadFromJsonAsync<PpoDecisionResponseDto>();
        return new PpoDecisionResponse(
            response!.CreditMultiplier,
            response.InterestDelta,
            response.LogProb,
            response.Value);
    }

    public async Task TrainAsync(object batchPayload)
    {
        var res = await http.PostAsJsonAsync("/rl/ppo/train", batchPayload);
        res.EnsureSuccessStatusCode();
    }

    private class MonteCarloResponseDto
    {
        public List<decimal> CreditMultipliers { get; set; } = new();
        public List<decimal> InterestDeltas { get; set; } = new();
        public List<decimal> Values { get; set; } = new();
        public List<decimal> LogProbs { get; set; } = new();
    }

    private class PpoDecisionResponseDto
    {
        public decimal CreditMultiplier { get; set; }
        public decimal InterestDelta { get; set; }
        public decimal LogProb { get; set; }
        public decimal Value { get; set; }
    }
}
