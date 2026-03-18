using System;
using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision;

public class PricingEngine : IPricingEngine
{
    public decimal CalculateRate(PricingContext ctx)
    {
        var baseRate = 0.15m; // 15%
        var spread = ctx.ProbabilityOfDefault * 0.5m;

        return Math.Clamp(baseRate + spread, 0.1m, 0.6m);
    }
}
