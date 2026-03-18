namespace Cobryx.Domain.Collections;

public class CollectionPolicy
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public int ReminderDays { get; private set; }
    public int CallDays { get; private set; }
    public int EscalationDays { get; private set; }
    public int LegalDays { get; private set; }

    private CollectionPolicy() { }

    public CollectionPolicy(Guid tenantId, int reminderDays, int callDays, int escalationDays, int legalDays)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        ReminderDays = reminderDays;
        CallDays = callDays;
        EscalationDays = escalationDays;
        LegalDays = legalDays;
    }
}
