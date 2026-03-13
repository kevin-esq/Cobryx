using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Events.Payments;

public record PaymentCompletedEvent(
    Guid PaymentId,
    Guid TenantId,
    Guid CustomerId,
    Money Amount,
    IReadOnlyCollection<PaymentAllocationEventData> Allocations,
    DateTime OccurredOn
) : IDomainEvent;

public record PaymentAllocationEventData(Guid InvoiceId, Money Amount);
