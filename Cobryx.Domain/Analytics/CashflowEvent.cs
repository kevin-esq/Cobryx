namespace Cobryx.Domain.Analytics;

public enum CashflowDirection
{
    Inflow = 1,
    Outflow = 2
}

public enum CashflowSource
{
    Payment = 1,
    LoanDisbursement = 2,
    LateFee = 3,
    Adjustment = 4
}

public class CashflowEvent
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public decimal Amount { get; private set; }
    public CashflowDirection Direction { get; private set; }
    public CashflowSource Source { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private CashflowEvent() { }

    public CashflowEvent(
        Guid tenantId,
        decimal amount,
        CashflowDirection direction,
        CashflowSource source,
        DateTime occurredAt)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Amount = amount;
        Direction = direction;
        Source = source;
        OccurredAt = occurredAt;
    }
}
