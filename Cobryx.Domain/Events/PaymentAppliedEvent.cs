using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Events;

public record PaymentAppliedEvent(
    Guid CreditId,
    Guid PaymentId,
    Money Amount,
    DateTime OccurredOn
) : IDomainEvent;
