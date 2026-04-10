using Cobryx.Domain.ML;

namespace Cobryx.Application.ML.Interfaces;

public interface IMonteCarloPpoClient
{
    public Task<MonteCarloResponse> EvaluateBatchAsync(
        object features,
        PortfolioState globalState,
        MacroState currentMacro,
        List<Scenario> scenarios);

    public Task<PpoDecisionResponse> DecideAsync(object state);
    public Task TrainAsync(object batchPayload);
}

public record MonteCarloResponse(
    List<decimal> CreditMultipliers,
    List<decimal> InterestDeltas,
    List<decimal> Values,
    List<decimal> LogProbs);

public record PpoDecisionResponse(
    decimal CreditMultiplier,
    decimal InterestDelta,
    decimal LogProb,
    decimal Value);
