using Cobryx.Domain.Shared;
using Cobryx.Domain.Shared.Enums;


namespace Cobryx.Domain.Identity;

public class BillingAlert : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public BillingAlertType AlertType { get; private set; }
    public string Message { get; private set; }
    public DateTime SentAt { get; private set; }
    public bool IsAcknowledged { get; private set; }

    private BillingAlert()
    {
        Message = null!;
    }

    public BillingAlert(Guid tenantId, BillingAlertType alertType, string message, DateTime? now = null)
    {
        TenantId = tenantId;
        AlertType = alertType;
        Message = message;
        SentAt = now ?? DateTime.UtcNow;
        IsAcknowledged = false;
    }

    public void Acknowledge()
    {
        IsAcknowledged = true;
        UpdateTimestamp();
    }
}
