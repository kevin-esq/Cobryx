using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision;

public class FraudEngine : IFraudEngine
{
    public decimal CalculateScore(FraudContext ctx)
    {
        var score = 0m;

        if (ctx.TransactionsLastHour > 10)
            score += 0.3m;

        if (ctx.AmountVelocity > 5000)
            score += 0.4m;

        if (ctx.GeoAnomaly)
            score += 0.5m;

        return Math.Clamp(score, 0m, 1m);
    }
}
