using Cobryx.Application.Collections.Models;
using Cobryx.Application.Collections.Optimizer;
using Cobryx.Application.Common.Interfaces;

namespace Cobryx.Application.Collections.Strategy
{
    public interface ICollectionsStrategyEngine
    {
        public CollectionDecision Evaluate(
            int daysPastDue,
            decimal outstanding,
            CustomerRiskProfile risk,
            PaymentBehaviorProfile behavior,
            DpdTrend trend,
            StrategyWeights weights,
            IClock clock);
    }
}
