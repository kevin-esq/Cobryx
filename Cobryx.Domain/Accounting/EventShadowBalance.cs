using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Accounting;

public class EventShadowBalance : BaseEntity
{
    public Guid TenantId { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal Balance { get; private set; }
    public long LastSequence { get; private set; }

    private EventShadowBalance() { }

    public EventShadowBalance(Guid tenantId, Guid accountId, decimal balance, long lastSequence)
    {
        TenantId = tenantId;
        AccountId = accountId;
        Balance = balance;
        LastSequence = lastSequence;
    }

    public void ApplyChange(decimal delta, long sequence)
    {
        if (sequence <= LastSequence) return;

        Balance += delta;
        LastSequence = sequence;
        UpdateTimestamp();
    }
}
