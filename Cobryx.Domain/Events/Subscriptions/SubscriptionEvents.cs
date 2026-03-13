using Cobryx.Domain.Shared;
using Cobryx.Domain.Shared.Enums;


namespace Cobryx.Domain.Events.Subscriptions;

public record SubscriptionTrialStartedEvent(
    Guid TenantId,
    Guid PlanId,
    DateTime TrialEndsAtUtc,
    DateTime OccurredOn
) : IDomainEvent;

public record SubscriptionActivatedEvent(
    Guid TenantId,
    Guid PlanId,
    DateTime OccurredOn
) : IDomainEvent;

public record SubscriptionPaymentFailedEvent(
    Guid TenantId,
    Guid PlanId,
    DateTime OccurredOn
) : IDomainEvent;

public record SubscriptionCancelledEvent(
    Guid TenantId,
    CancellationReason? Reason,
    DateTime OccurredOn
) : IDomainEvent;
