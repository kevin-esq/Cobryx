using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

/// <summary>
/// Record of a processed Stripe webhook event to ensure hard idempotency at the database level.
/// </summary>
public class ProcessedStripeEvent : BaseEntity, IAggregateRoot
{
    /// <summary>The unique ID delivered by Stripe (e.g., evt_123).</summary>
    public string StripeEventId { get; private set; } = default!;

    /// <summary>The Connected Account ID (null for platform events).</summary>
    public string? StripeAccountId { get; private set; }

    /// <summary>The type of the event (e.g., customer.subscription.updated).</summary>
    public string EventType { get; private set; } = default!;

    /// <summary>Timestamp when the event was successfully processed.</summary>
    public DateTime ProcessedAtUtc { get; private set; }

    private ProcessedStripeEvent() { }

    public ProcessedStripeEvent(string stripeEventId, string eventType, DateTime processedAtUtc, string? stripeAccountId = null)
    {
        StripeEventId = stripeEventId;
        EventType = eventType;
        ProcessedAtUtc = processedAtUtc;
        StripeAccountId = stripeAccountId;
    }
}
