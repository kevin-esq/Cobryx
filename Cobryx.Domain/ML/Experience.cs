namespace Cobryx.Domain.ML;

/// <summary>
/// Experience tuple for reinforcement learning training.
/// </summary>
public class Experience
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string StateJson { get; set; } = string.Empty;
    public decimal CreditMultiplier { get; set; }
    public decimal InterestDelta { get; set; }
    public decimal LogProb { get; set; }
    public decimal Value { get; set; }
    public decimal Reward { get; set; }
    public string NextStateJson { get; set; } = string.Empty;
    public bool Done { get; set; }
    public string Source { get; set; } = "production";
}
