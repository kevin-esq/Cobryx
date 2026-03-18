namespace Cobryx.Domain.Analytics.Risk;

public class RiskEvent
{
    public System.Guid Id { get; private set; }
    public System.Guid CustomerId { get; private set; }

    public RiskEventType EventType { get; private set; }
    public decimal ImpactScore { get; private set; }

    public System.DateTime OccurredAt { get; private set; }

    private RiskEvent() { }

    public RiskEvent(System.Guid customerId, RiskEventType eventType, decimal impactScore)
    {
        Id = System.Guid.NewGuid();
        CustomerId = customerId;
        EventType = eventType;
        ImpactScore = impactScore;
        OccurredAt = System.DateTime.UtcNow;
    }
}

public enum RiskEventType
{
    MissedPayment,
    LatePayment,
    BalanceIncrease,
    ManualAdjustment
}
