using Cobryx.Application.Decision;

namespace Cobryx.Application.Tests.Decision;

public class DecisionEngineTests
{
    private readonly DecisionEngine _engine;

    public DecisionEngineTests()
    {
        _engine = new DecisionEngine(
            new CreditLimitEngine(),
            new PricingEngine(),
            new FraudEngine());
    }

    [Fact]
    public void HighRisk_ShouldLowerLimit_AndIncreaseRate()
    {
        var ctx = new DecisionContext
        {
            Credit = new CreditContext
            {
                ProbabilityOfDefault = 0.8m,
                BehaviorScore = 0.5m,
                MonthlyIncomeEstimate = 10000
            },
            Pricing = new PricingContext
            {
                ProbabilityOfDefault = 0.8m
            },
            Fraud = new FraudContext()
        };

        var result = _engine.Evaluate(ctx);

        Assert.True(result.CreditLimit < 10000);
        Assert.True(result.InterestRate > 0.3m);
    }

    [Fact]
    public void HighFraud_ShouldReject()
    {
        var ctx = new DecisionContext
        {
            Credit = new CreditContext
            {
                ProbabilityOfDefault = 0.2m,
                BehaviorScore = 1m,
                MonthlyIncomeEstimate = 10000
            },
            Pricing = new PricingContext
            {
                ProbabilityOfDefault = 0.2m
            },
            Fraud = new FraudContext
            {
                TransactionsLastHour = 20,
                AmountVelocity = 10000,
                GeoAnomaly = true
            }
        };

        var result = _engine.Evaluate(ctx);

        Assert.False(result.Approved);
    }

    [Fact]
    public void HighPD_ShouldIncreaseRateExponentially()
    {
        var pricingEngine = new PricingEngine();
        var lowRiskCtx = new PricingContext { ProbabilityOfDefault = 0.1m };
        var highRiskCtx = new PricingContext { ProbabilityOfDefault = 0.8m };

        var rateLow = pricingEngine.CalculateRate(lowRiskCtx);
        var rateHigh = pricingEngine.CalculateRate(highRiskCtx);

        Assert.True(rateHigh > rateLow * 2);
    }

    [Fact]
    public void HighUtilization_ShouldReduceLimit()
    {
        var creditEngine = new CreditLimitEngine();

        var lowUtilCtx = new CreditContext { MonthlyIncomeEstimate = 10000m, BehaviorScore = 1m, ProbabilityOfDefault = 0.1m, Utilization = 0.1m };
        var highUtilCtx = new CreditContext { MonthlyIncomeEstimate = 10000m, BehaviorScore = 1m, ProbabilityOfDefault = 0.1m, Utilization = 0.9m };

        var limitLow = creditEngine.Calculate(lowUtilCtx);
        var limitHigh = creditEngine.Calculate(highUtilCtx);

        Assert.True(limitHigh < limitLow);
    }
}
