using Cobryx.Domain.Decision;
using Cobryx.Domain.ML;

namespace Cobryx.Application.Decision;

public interface IRiskEvaluator
{
    Task<(decimal pd, string modelVersion)> EvaluateRiskAsync(Guid customerId, DecisionContext ctx, FeatureVector features);
}
