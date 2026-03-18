namespace Cobryx.Application.Risk.Factors;

public class TrendRiskFactor : IRiskFactor
{
    public decimal Evaluate(RiskContext context)
    {
        var deltaUtil = context.Utilization - context.PreviousUtilization;
        var deltaDelay = context.PaymentDelayDays - context.PreviousPaymentDelayDays;

        var score = deltaUtil + (deltaDelay / 30m);

        return Math.Max(0, score);
    }
}
