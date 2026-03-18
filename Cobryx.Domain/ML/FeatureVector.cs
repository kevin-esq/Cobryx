namespace Cobryx.Domain.ML;

public class FeatureVector
{
    public decimal PD { get; set; }
    public decimal Utilization { get; set; }
    public decimal PaymentDelay { get; set; }
    public decimal BehaviorScore { get; set; }
    public decimal DpdTrend { get; set; }
}
