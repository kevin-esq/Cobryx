namespace Cobryx.Domain.ML.Simulation;

public class StepResult
{
    public decimal Reward { get; set; }
    public bool Done { get; set; }
    public EconomyState NextState { get; set; } = new();
}
