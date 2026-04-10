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

    public CustomerRiskProfile(Guid customerId, DateTime? now = null)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        LastUpdated = now ?? DateTime.UtcNow;
    }

    public void UpdateScores(decimal risk, decimal behavior, decimal credit, decimal pd)
        => UpdateScores(risk, behavior, credit, pd, DateTime.UtcNow);

    public void UpdateScores(decimal risk, decimal behavior, decimal credit, decimal pd, DateTime now)
    {
        RiskScore = risk;
        BehaviorScore = behavior;
        CreditScore = credit;
        ProbabilityOfDefault = System.Math.Clamp(pd, 0m, 1m);
        LastUpdated = now;
    }
}
