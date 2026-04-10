namespace Cobryx.Domain.ML;

public class DecisionDistributionLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CustomerId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public decimal[] CreditMultipliers { get; private set; } = [];
    public decimal[] InterestDeltas { get; private set; } = [];
    public decimal VaR95 { get; private set; }
    public decimal CVaR95 { get; private set; }
    public string ScenarioSetJson { get; private set; } = string.Empty;

    private DecisionDistributionLog() { }

    public DecisionDistributionLog(
        Guid customerId,
        decimal[] creditMultipliers,
        decimal[] interestDeltas,
        decimal var95,
        decimal cVar95,
        string scenarioSetJson,
        DateTime? timestamp = null)
    {
        CustomerId = customerId;
        CreditMultipliers = creditMultipliers;
        InterestDeltas = interestDeltas;
        VaR95 = var95;
        CVaR95 = cVar95;
        ScenarioSetJson = scenarioSetJson;
        Timestamp = timestamp ?? DateTime.UtcNow;
    }
}
