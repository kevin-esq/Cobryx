using System.Net.Http.Json;

using Cobryx.Application.ML.Interfaces;
using Cobryx.Domain.ML;

namespace Cobryx.Infrastructure.ML.Clients;

public class PpoClient(HttpClient http) : IPpoClient
{
    public async Task<PpoResponse> DecideAsync(object features)
    {
        var res = await http.PostAsJsonAsync("/rl/ppo/decide", features);
        res.EnsureSuccessStatusCode();

        var response = await res.Content.ReadFromJsonAsync<PpoResponseDto>();
        return new PpoResponse(
            response!.CreditMultiplier,
            response.InterestDelta,
            response.LogProb,
            response.Value);
    }

    public async Task<CombinedPpoResponse> DecideCombinedAsync(object payload)
    {
        var res = await http.PostAsJsonAsync("/rl/ppo/combined", payload);
        res.EnsureSuccessStatusCode();

        var response = await res.Content.ReadFromJsonAsync<CombinedPpoResponseDto>();
        return new CombinedPpoResponse(
            response!.Portfolio,
            new PpoResponse(
                response.Local.CreditMultiplier,
                response.Local.InterestDelta,
                response.Local.LogProb,
                response.Local.Value));
    }

    private class PpoResponseDto
    {
        public decimal CreditMultiplier { get; set; }
        public decimal InterestDelta { get; set; }
        public decimal LogProb { get; set; }
        public decimal Value { get; set; }
    }

    private class CombinedPpoResponseDto
    {
        public PortfolioAction Portfolio { get; set; } = new();
        public PpoResponseDto Local { get; set; } = new();
    }
}
