using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;

namespace Cobryx.Domain.Entities;

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

    public BillingAlert(Guid tenantId, BillingAlertType alertType, string message)
    {
        TenantId = tenantId;
        AlertType = alertType;
        Message = message;
        SentAt = DateTime.UtcNow;
        IsAcknowledged = false;
    }

    public void Acknowledge()
    {
        IsAcknowledged = true;
        UpdateTimestamp();
    }
}
