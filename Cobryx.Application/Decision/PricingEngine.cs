using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision;

public class PricingEngine : IPricingEngine
{
    public decimal CalculateRate(PricingContext ctx)
    {
        var baseRate = 0.15m;
        var riskPremium = (decimal)System.Math.Pow((double)ctx.ProbabilityOfDefault, 1.5);

        var rate = baseRate + (riskPremium * 0.6m);

        return System.Math.Clamp(rate, 0.1m, 0.6m);
    }
}
