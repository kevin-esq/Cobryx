using Cobryx.Domain.ML;

namespace Cobryx.Application.ML.Interfaces;

public interface IPpoClient
{
    public Task<PpoResponse> DecideAsync(object features);
    public Task<CombinedPpoResponse> DecideCombinedAsync(object payload);
}

public record PpoResponse(
    decimal CreditMultiplier,
    decimal InterestDelta,
    decimal LogProb,
    decimal Value);

public record CombinedPpoResponse(
    PortfolioAction Portfolio,
    PpoResponse Local);
