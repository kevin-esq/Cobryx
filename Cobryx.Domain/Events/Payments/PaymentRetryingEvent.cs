using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Events.Payments;

public record PaymentRetryingEvent(
    Guid PaymentId,
    Guid TenantId,
    int AttemptNumber,
    DateTime OccurredOn
) : IDomainEvent;
