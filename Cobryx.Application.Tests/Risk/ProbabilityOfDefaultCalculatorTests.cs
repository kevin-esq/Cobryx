using Cobryx.Application.Risk;
using Cobryx.Application.Risk.Factors;

namespace Cobryx.Application.Tests.Risk;

public class ProbabilityOfDefaultCalculatorTests
{
    private readonly ProbabilityOfDefaultCalculator _calculator;

    public ProbabilityOfDefaultCalculatorTests()
    {
        _calculator = new ProbabilityOfDefaultCalculator(new (IRiskFactor, decimal)[]
        {
            (new DpdRiskFactor(), 0.4m),
            (new UtilizationRiskFactor(), 0.2m),
            (new PaymentDelayRiskFactor(), 0.2m),
            (new TrendRiskFactor(), 0.2m)
        });
    }

    [Fact]
    public void PD_HighDpd_ShouldBe_GreaterThan_LowDpd()
    {
        var highDpdContext = new RiskContext { DaysPastDue = 90 };
        var lowDpdContext = new RiskContext { DaysPastDue = 15 };

        var pdHigh = _calculator.Calculate(highDpdContext);
        var pdLow = _calculator.Calculate(lowDpdContext);

        Assert.True(pdHigh > pdLow);
    }

    [Fact]
    public void Trend_HighDeltaDelay_Increases_PD()
    {
        var baselineContext = new RiskContext
        {
            DaysPastDue = 30,
            PaymentDelayDays = 30,
            PreviousPaymentDelayDays = 30
        };

        var worseningContext = new RiskContext
        {
            DaysPastDue = 30,
            PaymentDelayDays = 60,
            PreviousPaymentDelayDays = 30
        };

        var pdBaseline = _calculator.Calculate(baselineContext);
        var pdWorsening = _calculator.Calculate(worseningContext);

        Assert.True(pdWorsening > pdBaseline);
    }

    [Fact]
    public void PD_Is_Clamped_Between_0_and_1()
    {
        var extremeContext = new RiskContext
        {
            DaysPastDue = 900,
            Utilization = 5m,
            CreditLimit = 1,
            Outstanding = 5,
            PaymentDelayDays = 300,
            PreviousPaymentDelayDays = 0,
            PreviousUtilization = 0
        };

        var pd = _calculator.Calculate(extremeContext);

        Assert.InRange(pd, 0m, 1m);
    }
}
