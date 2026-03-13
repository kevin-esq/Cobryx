using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Events.Payments;

public record PaymentRefundedEvent(
    Guid PaymentId,
    Guid TenantId,
    Money Amount,
    DateTime OccurredOn
) : IDomainEvent;
