namespace Cobryx.Domain.ML;

/// <summary>
/// Reinforcement Learning state representation.
/// </summary>
public class RlState
{
    public decimal PdBucket { get; set; }
    public decimal UtilizationBucket { get; set; }
    public decimal BehaviorBucket { get; set; }

    public string ToKey() => $"{PdBucket}:{UtilizationBucket}:{BehaviorBucket}";
}
