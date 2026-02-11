using Cobryx.Domain.Common;

namespace Cobryx.Domain.Events.Invoicing;

public record InvoicePaidEvent(
    Guid InvoiceId,
    Guid TenantId,
    Guid CustomerId,
    DateTime OccurredOn
) : IDomainEvent;
