using Cobryx.Domain.Common;

namespace Cobryx.Domain.Events.Lending;

public record LoanCreatedEvent(
    Guid LoanId,
    Guid TenantId,
    Guid CustomerId,
    DateTime OccurredOn
) : IDomainEvent;
