using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision;

public class DecisionEngine
{
    private readonly ICreditLimitEngine _limit;
    private readonly IPricingEngine _pricing;
    private readonly IFraudEngine _fraud;

    public DecisionEngine(
        ICreditLimitEngine limit,
        IPricingEngine pricing,
        IFraudEngine fraud)
    {
        _limit = limit;
        _pricing = pricing;
        _fraud = fraud;
    }

    public DecisionResult Evaluate(DecisionContext ctx)
    {
        var limit = _limit.Calculate(ctx.Credit);
        var rate = _pricing.CalculateRate(ctx.Pricing);
        var fraud = _fraud.CalculateScore(ctx.Fraud);

        return new DecisionResult
        {
            CreditLimit = limit,
            InterestRate = rate,
            FraudScore = fraud,
            Approved = fraud < 0.7m && ctx.Credit.ProbabilityOfDefault < 0.8m
        };
    }
}
