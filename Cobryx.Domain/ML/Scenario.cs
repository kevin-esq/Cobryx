namespace Cobryx.Domain.ML;

/// <summary>
/// Economic scenario for stress testing.
/// </summary>
public class Scenario
{
    public string Name { get; set; } = "Stochastic";
    public decimal Inflation { get; set; }
    public decimal InterestRate { get; set; }
    public decimal DefaultRate { get; set; }
    public decimal LiquidityShock { get; set; }
}
