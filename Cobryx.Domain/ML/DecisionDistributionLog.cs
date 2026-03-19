namespace Cobryx.Domain.ML;

public class DecisionDistributionLog
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime Timestamp { get; set; }

    public decimal[] CreditMultipliers { get; set; } = [];
    public decimal[] InterestDeltas { get; set; } = [];

    public decimal VaR95 { get; set; }
    public decimal CVaR95 { get; set; }

    public string ScenarioSetJson { get; set; } = "";
}
