namespace Cobryx.Domain.Analytics.Risk;

public class CustomerRiskSnapshot
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    public decimal RiskScore { get; private set; }
    public decimal BehaviorScore { get; private set; }
    public decimal ProbabilityOfDefault { get; private set; }

    public DateTime RecordedAt { get; private set; }

    private CustomerRiskSnapshot() { }

    public CustomerRiskSnapshot(System.Guid customerId, decimal riskScore, decimal behaviorScore, decimal pd)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        RiskScore = riskScore;
        BehaviorScore = behaviorScore;
        ProbabilityOfDefault = pd;
        RecordedAt = DateTime.UtcNow;
    }
}
