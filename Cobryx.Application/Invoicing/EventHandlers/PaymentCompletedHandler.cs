using Cobryx.Application.Common.Events;
using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Interfaces;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Invoicing.EventHandlers;

public class PaymentCompletedHandler(
    IPaymentRepository paymentRepository,
    IInvoiceRepository invoiceRepository,
    ILogger<PaymentCompletedHandler> logger) : INotificationHandler<DomainEventNotification<PaymentCompletedEvent>>
{
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository = invoiceRepository;
    private readonly ILogger<PaymentCompletedHandler> _logger = logger;

    public async Task Handle(DomainEventNotification<PaymentCompletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        _logger.LogInformation("Processing PaymentCompletedEvent for Payment {PaymentId} with {AllocationCount} allocations",
            domainEvent.PaymentId, domainEvent.Allocations.Count);

        foreach (var allocation in domainEvent.Allocations)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(allocation.InvoiceId, cancellationToken);
            if (invoice == null)
            {
                _logger.LogWarning("Invoice {InvoiceId} not found for allocation from Payment {PaymentId}",
                    allocation.InvoiceId, domainEvent.PaymentId);
                continue;
            }

            _logger.LogInformation("Applying {Amount} {Currency} to Invoice {InvoiceId} from Payment {PaymentId}",
                allocation.Amount.Amount, allocation.Amount.Currency, invoice.Id, domainEvent.PaymentId);

            invoice.ApplyPayment(domainEvent.PaymentId, allocation.Amount);
            await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        }
    }
}
