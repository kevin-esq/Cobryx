namespace Cobryx.Application.Risk.Factors;

public class UtilizationRiskFactor : IRiskFactor
{
    public decimal Evaluate(RiskContext context)
    {
        // TODO: replace with real utilization once credit limits are available
        var utilization = context.CreditLimit > 0
            ? context.Outstanding / context.CreditLimit
            : 0m;

        return utilization switch
        {
            > 0.9m => 1.0m,
            > 0.75m => 0.7m,
            > 0.5m => 0.4m,
            _ => 0.1m
        };
    }
}
