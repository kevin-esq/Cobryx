namespace Cobryx.Domain.ML;

/// <summary>
/// Feature vector for ML model input.
/// </summary>
public class FeatureVector
{
    public decimal Utilization { get; set; }
    public decimal PaymentDelay { get; set; }
    public decimal BehaviorScore { get; set; }
    public decimal DpdTrend { get; set; }
    public decimal Outstanding { get; set; }
}
