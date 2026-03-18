namespace Cobryx.Domain.Decision;

public class DecisionSnapshot
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    public decimal ProbabilityOfDefault { get; private set; }
    public decimal CreditLimit { get; private set; }
    public decimal InterestRate { get; private set; }
    public decimal FraudScore { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private DecisionSnapshot() { }

    public DecisionSnapshot(
        Guid customerId,
        decimal pd,
        decimal limit,
        decimal rate,
        decimal fraudScore)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        ProbabilityOfDefault = pd;
        CreditLimit = limit;
        InterestRate = rate;
        FraudScore = fraudScore;
        CreatedAt = DateTime.UtcNow;
    }
}
