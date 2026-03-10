using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Accounting.Enums;

namespace Cobryx.Domain.Entities.Accounting;

/// <summary>
/// Storage for financial events that failed publication after maximum retries.
/// Allows for manual investigation and replay without blocking the main pipeline.
/// </summary>
public class DeadLetterEvent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid EventId { get; private set; }
    public FinancialEventType Type { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;
    public DateTime FailedAt { get; private set; }

    private DeadLetterEvent() { }

    public DeadLetterEvent(FinancialOutboxEvent @event, string error)
    {
        TenantId = @event.TenantId;
        EventId = @event.Id;
        Type = @event.Type;
        PayloadJson = @event.PayloadJson;
        ErrorMessage = error;
        FailedAt = DateTime.UtcNow;
    }
}
