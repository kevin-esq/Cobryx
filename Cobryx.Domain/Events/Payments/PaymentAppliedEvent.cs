using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Events.Payments;

public record PaymentAppliedEvent(
    Guid CreditId,
    Guid PaymentId,
    Money Amount,
    DateTime OccurredOn
) : IDomainEvent;
