using Cobryx.Application.Collections.Models;

namespace Cobryx.Application.Collections.Strategy;

public interface ICollectionsStrategyEngine
{
    public CollectionDecision Evaluate(
        int daysPastDue,
        decimal outstanding,
        CustomerRiskProfile risk,
        PaymentBehaviorProfile behavior,
        DpdTrend trend,
        Cobryx.Application.Collections.Optimizer.StrategyWeights weights);
}
