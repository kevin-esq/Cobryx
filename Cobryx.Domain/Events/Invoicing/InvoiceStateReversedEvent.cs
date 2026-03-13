using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Events.Invoicing;

public record InvoiceStateReversedEvent(
    Guid InvoiceId,
    Guid TenantId,
    InvoiceStatus NewStatus,
    Money NewTotalPaid,
    DateTime OccurredOn
) : IDomainEvent;
