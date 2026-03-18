namespace Cobryx.Application.Risk;

public interface IRiskFactor
{
    public decimal Evaluate(RiskContext context);
}
