using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Events.Payments;

public record PaymentChargebackedEvent(
    Guid PaymentId,
    Guid TenantId,
    DateTime OccurredOn
) : IDomainEvent;
