using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Interfaces;
using Cobryx.Application.Common.Events;
using Concordia;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Invoicing.EventHandlers;

public class PaymentChargebackedHandler : INotificationHandler<DomainEventNotification<PaymentChargebackedEvent>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<PaymentChargebackedHandler> _logger;

    public PaymentChargebackedHandler(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        ILogger<PaymentChargebackedHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PaymentChargebackedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        _logger.LogWarning("Processing PaymentChargebackedEvent for Payment {PaymentId}. Reversing all allocations.", 
            domainEvent.PaymentId);

        var payment = await _paymentRepository.GetByIdAsync(domainEvent.PaymentId, cancellationToken);
        if (payment == null)
        {
            _logger.LogError("Payment {PaymentId} not found during chargeback processing.", domainEvent.PaymentId);
            return;
        }

        foreach (var allocation in payment.Allocations)
        {
            if (!allocation.IsReversed) 
            {
                continue;
            }

            var invoice = await _invoiceRepository.GetByIdAsync(allocation.InvoiceId, cancellationToken);
            if (invoice == null)
            {
                _logger.LogWarning("Invoice {InvoiceId} not found during chargeback reversal of Payment {PaymentId}", 
                    allocation.InvoiceId, domainEvent.PaymentId);
                continue;
            }

            _logger.LogInformation("Reversing allocation of {Amount} from Invoice {InvoiceId} due to Chargeback of Payment {PaymentId}",
                allocation.Amount, invoice.Id, domainEvent.PaymentId);

            invoice.ReverseAllocation(allocation);
            await _invoiceRepository.UpdateAsync(invoice);
        }
    }
}
