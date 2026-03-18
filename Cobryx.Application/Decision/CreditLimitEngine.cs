using System;
using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision;

public class CreditLimitEngine : ICreditLimitEngine
{
    public decimal Calculate(CreditContext ctx)
    {
        var baseLimit = ctx.MonthlyIncomeEstimate * 2m;
        var behaviorMultiplier = 0.5m + ctx.BehaviorScore; // 0.5 - 1.5

        var limit = baseLimit * (1 - ctx.ProbabilityOfDefault) * behaviorMultiplier;

        return Math.Clamp(limit, 100m, 50000m);
    }
}
