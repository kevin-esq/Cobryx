using Cobryx.Application.Common.Events;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Payments;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Invoicing.EventHandlers
{
    public class PaymentChargebackedHandler(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        ILogger<PaymentChargebackedHandler> logger)
        : INotificationHandler<DomainEventNotification<PaymentChargebackedEvent>>
    {
        public async Task Handle(DomainEventNotification<PaymentChargebackedEvent> notification,
            CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;
            logger.LogWarning("Processing PaymentChargebackedEvent for Payment {PaymentId}. Reversing all allocations.",
                domainEvent.PaymentId);

            Payment? payment = await paymentRepository.GetByIdAsync(domainEvent.PaymentId, cancellationToken);
            if (payment == null)
            {
                logger.LogError("Payment {PaymentId} not found during chargeback processing.", domainEvent.PaymentId);
                return;
            }

            foreach (PaymentAllocation allocation in payment.Allocations)
            {
                Invoice? invoice = await invoiceRepository.GetByIdAsync(allocation.InvoiceId, cancellationToken);
                if (invoice == null)
                {
                    continue;
                }

                invoice.ReverseAllocation(allocation, allocation.Amount);
                await invoiceRepository.UpdateAsync(invoice, cancellationToken);
            }
        }
    }
}
