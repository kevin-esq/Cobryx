using Cobryx.Application.Common.Events;
using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Interfaces;

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
            var invoice = await _invoiceRepository.GetByIdAsync(allocation.InvoiceId, cancellationToken);
            if (invoice != null)
            {
                invoice.ReverseAllocation(allocation);
                await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
            }
        }
    }
}
