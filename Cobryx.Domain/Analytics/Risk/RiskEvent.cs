namespace Cobryx.Domain.Analytics.Risk;

public class RiskEvent
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    public string EventType { get; private set; } = string.Empty;
    public decimal ImpactScore { get; private set; }

    public DateTime OccurredAt { get; private set; }

    private RiskEvent() { }

    public RiskEvent(Guid customerId, string eventType, decimal impactScore)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        EventType = eventType;
        ImpactScore = impactScore;
        OccurredAt = DateTime.UtcNow;
    }
}
