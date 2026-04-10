namespace Cobryx.Application.Risk.Factors;

public class PaymentDelayRiskFactor : IRiskFactor
{
    public decimal Evaluate(RiskContext context)
    {
        if (context.PaymentDelayDays <= 0)
            return 0m;

        return Math.Min(1.0m, context.PaymentDelayDays / 30m);
    }
}
