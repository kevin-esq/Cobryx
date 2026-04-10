using Cobryx.Application.Risk;
using Cobryx.Application.Risk.Factors;

namespace Cobryx.Application.Tests.Unit.Risk;

public class EarlyWarningJobTests
{
    [Fact]
    public void NonDelinquentHighPD_ShouldTriggerEarlyWarning()
    {
        var context = new RiskContext
        {
            DaysPastDue = 0,
            Outstanding = 1000m,
            CreditLimit = 1000m,
            Utilization = 1m,
            PaymentDelayDays = 30
        };

        var calculator = new ProbabilityOfDefaultCalculator(new (IRiskFactor, decimal)[]
        {
            (new DpdRiskFactor(), 0.4m),
            (new UtilizationRiskFactor(), 0.2m),
            (new PaymentDelayRiskFactor(), 0.2m),
            (new TrendRiskFactor(), 0.2m)
        });

        var pd = calculator.Calculate(context);

        Assert.True(pd > 0.1m);
    }
}
