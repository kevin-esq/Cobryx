using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision;

public class CreditLimitEngine : ICreditLimitEngine
{
    public decimal Calculate(CreditContext ctx)
    {
        var baseLimit = ctx.MonthlyIncomeEstimate * 2m;
        var behaviorMultiplier = 0.5m + ctx.BehaviorScore;

        var limit =
            baseLimit
          * (1 - ctx.ProbabilityOfDefault)
          * behaviorMultiplier
          * (1 - ctx.Utilization);

        return System.Math.Clamp(limit, 100m, 50000m);
    }
}
