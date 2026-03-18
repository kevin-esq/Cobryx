namespace Cobryx.Domain.Analytics.Risk;

public class CustomerRiskSnapshot
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    public decimal ProbabilityOfDefault { get; private set; }

    public DateTime RecordedAt { get; private set; }

    private CustomerRiskSnapshot() { }

    public CustomerRiskSnapshot(Guid customerId, decimal pd)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        ProbabilityOfDefault = pd;
        RecordedAt = DateTime.UtcNow;
    }
}
