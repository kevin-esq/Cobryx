namespace Cobryx.Domain.ML;

/// <summary>
/// Outcome of a credit decision for ML training.
/// </summary>
public class DecisionOutcome
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string StateKey { get; set; } = string.Empty;
    public DecisionAction Action { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal InterestRate { get; set; }
    public bool Defaulted { get; set; }
    public decimal AmountRecovered { get; set; }
    public decimal Reward { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
