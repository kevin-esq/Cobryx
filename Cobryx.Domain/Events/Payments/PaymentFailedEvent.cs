using Cobryx.Domain.Common;

namespace Cobryx.Domain.Events.Payments;

public record PaymentFailedEvent(
    Guid PaymentId,
    Guid TenantId,
    Guid CustomerId,
    string Reason,
    DateTime OccurredOn
) : IDomainEvent;
