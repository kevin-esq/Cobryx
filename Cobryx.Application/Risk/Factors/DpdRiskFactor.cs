namespace Cobryx.Application.Risk.Factors;

public class DpdRiskFactor : IRiskFactor
{
    public decimal Evaluate(RiskContext context)
    {
        return context.DaysPastDue switch
        {
            >= 90 => 1.0m,
            >= 60 => 0.8m,
            >= 30 => 0.5m,
            > 0 => 0.2m,
            _ => 0m
        };
    }
}
