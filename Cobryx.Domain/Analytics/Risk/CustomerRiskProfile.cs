namespace Cobryx.Domain.Analytics.Risk;

public class CustomerRiskProfile
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    public decimal RiskScore { get; private set; }
    public decimal BehaviorScore { get; private set; }
    public decimal CreditScore { get; private set; }

    public decimal ProbabilityOfDefault { get; private set; }

    public DateTime LastUpdated { get; private set; }

    private CustomerRiskProfile() { }

    public CustomerRiskProfile(Guid customerId)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateScores(decimal risk, decimal behavior, decimal credit, decimal pd)
    {
        RiskScore = risk;
        BehaviorScore = behavior;
        CreditScore = credit;
        ProbabilityOfDefault = pd;
        LastUpdated = DateTime.UtcNow;
    }
}
