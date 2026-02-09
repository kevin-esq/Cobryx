using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Events.Invoicing;

public record InvoiceStateReversedEvent(
    Guid InvoiceId,
    Guid TenantId,
    InvoiceStatus NewStatus,
    Money NewTotalPaid,
    DateTime OccurredOn
) : IDomainEvent;
