namespace Cobryx.Application.Collections.Optimizer;

public class StrategyWeights
{
    public decimal SmsWeight { get; set; } = 1.0m;
    public decimal CallWeight { get; set; } = 1.0m;
    public decimal EmailWeight { get; set; } = 1.0m;
    public decimal LegalWeight { get; set; } = 1.0m;
}

public interface ICollectionOptimizer
{
    public System.Threading.Tasks.Task<StrategyWeights> CalculateWeightsAsync(System.Guid tenantId);
}
