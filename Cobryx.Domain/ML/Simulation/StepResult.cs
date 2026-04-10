namespace Cobryx.Domain.ML.Simulation;

/// <summary>
/// Result of a simulation step.
/// </summary>
public class StepResult
{
    public decimal Reward { get; set; }
    public bool Done { get; set; }
    public EconomyState NextState { get; set; } = new();
}
