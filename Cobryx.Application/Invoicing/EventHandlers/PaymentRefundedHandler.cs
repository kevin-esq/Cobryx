using Cobryx.Application.Common.Events;
using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Interfaces;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Invoicing.EventHandlers;

public class PaymentRefundedHandler : INotificationHandler<DomainEventNotification<PaymentRefundedEvent>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<PaymentRefundedHandler> _logger;

    public PaymentRefundedHandler(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        ILogger<PaymentRefundedHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<PaymentRefundedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        _logger.LogInformation("Processing PaymentRefundedEvent for Payment {PaymentId}. Amount: {Amount}",
            domainEvent.PaymentId, domainEvent.Amount);

        var payment = await _paymentRepository.GetByIdAsync(domainEvent.PaymentId, cancellationToken);
        if (payment == null) return;
        if (payment.IsFullyRefunded)
        {
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
        else
        {
            _logger.LogWarning("Partial refund detected for Payment {PaymentId}. Manual intervention or refined allocation reversal strategy required.", domainEvent.PaymentId);
        }
    }
}
