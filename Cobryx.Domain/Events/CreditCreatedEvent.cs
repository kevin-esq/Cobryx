using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Events;

public record CreditCreatedEvent(
    Guid CreditId,
    Guid TenantId,
    Guid CustomerId,
    Money Principal,
    DateTime OccurredOn
) : IDomainEvent;
