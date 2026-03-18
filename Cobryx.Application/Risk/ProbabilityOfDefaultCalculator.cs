namespace Cobryx.Application.Risk;

public class ProbabilityOfDefaultCalculator
{
    private readonly System.Collections.Generic.IEnumerable<(IRiskFactor Factor, decimal Weight)> _factors;

    public ProbabilityOfDefaultCalculator(System.Collections.Generic.IEnumerable<(IRiskFactor Factor, decimal Weight)> factors)
    {
        _factors = factors;
    }

    public decimal Calculate(RiskContext context)
    {
        var totalWeight = System.Linq.Enumerable.Sum(_factors, f => f.Weight);
        var weightedSum = System.Linq.Enumerable.Sum(_factors, f => f.Factor.Evaluate(context) * f.Weight);

        var pd = totalWeight > 0 ? weightedSum / totalWeight : 0m;

        return System.Math.Clamp(pd, 0m, 1m);
    }
}
