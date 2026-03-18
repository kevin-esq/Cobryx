namespace Cobryx.Domain.ML;

public class RlState
{
    public decimal PdBucket { get; set; }
    public decimal UtilizationBucket { get; set; }
    public decimal BehaviorBucket { get; set; }

    public string ToKey() => $"{PdBucket}:{UtilizationBucket}:{BehaviorBucket}";
}
