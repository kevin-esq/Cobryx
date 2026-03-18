using Cobryx.Application.Collections.Models;
using Cobryx.Domain.Analytics;

namespace Cobryx.Application.Collections.Strategy;

public interface ICollectionsStrategyEngine
{
    public CollectionDecision Evaluate(
        LoanBalanceSnapshot snapshot,
        CustomerRiskProfile risk,
        PaymentBehaviorProfile behavior,
        DpdTrend trend);
}
