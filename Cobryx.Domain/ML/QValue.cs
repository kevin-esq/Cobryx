namespace Cobryx.Domain.ML;

/// <summary>
/// Q-value for reinforcement learning.
/// </summary>
public class QValue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StateKey { get; set; } = string.Empty;
    public DecisionAction Action { get; set; }
    public decimal Value { get; set; }
}
