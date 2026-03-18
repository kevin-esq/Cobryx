namespace Cobryx.Domain.ML;

public class FeatureVector
{
    public decimal Utilization { get; set; }
    public decimal PaymentDelay { get; set; }
    public decimal BehaviorScore { get; set; }
    public decimal DpdTrend { get; set; }
    public decimal Outstanding { get; set; }
}
