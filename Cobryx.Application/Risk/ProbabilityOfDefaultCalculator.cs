namespace Cobryx.Application.Risk;

public class ProbabilityOfDefaultCalculator
{
    private readonly IEnumerable<IRiskFactor> _factors;

    public ProbabilityOfDefaultCalculator(IEnumerable<IRiskFactor> factors)
    {
        _factors = factors;
    }

    public decimal Calculate(RiskContext context)
    {
        var scores = _factors.Select(f => f.Evaluate(context)).ToList();

        var pd = scores.Average(); // simple aggregation

        return Math.Min(1.0m, pd);
    }
}
