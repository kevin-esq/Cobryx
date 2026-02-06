using Cobryx.Domain.Common;

namespace Cobryx.Domain.Events.Invoicing;

public record InvoiceIssuedEvent(
    Guid InvoiceId,
    Guid TenantId,
    Guid CustomerId,
    DateTime OccurredOn
) : IDomainEvent;
