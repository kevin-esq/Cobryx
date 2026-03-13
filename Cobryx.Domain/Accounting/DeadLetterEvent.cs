using Cobryx.Domain.Messaging;
using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Accounting;

/// <summary>
/// Storage for financial events that failed publication after maximum retries.
/// Allows for manual investigation and replay without blocking the main pipeline.
/// </summary>
public class DeadLetterEvent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid EventId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;
    public DateTime FailedAt { get; private set; }

    private DeadLetterEvent() { }

    public DeadLetterEvent(OutboxMessage @event, string error)
    {
        TenantId = @event.TenantId;
        EventId = @event.Id;
        Type = @event.Type;
        Payload = @event.Payload;
        ErrorMessage = error;
        FailedAt = DateTime.UtcNow;
    }
}
