namespace Cobryx.Domain.ML.Simulation;

/// <summary>
/// Action taken in the economy simulation.
/// </summary>
public class EconomyAction
{
    public decimal CreditMultiplier { get; set; }
    public decimal InterestDelta { get; set; }
}
