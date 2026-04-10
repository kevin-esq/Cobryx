using Cobryx.Application.Common.Events;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Interfaces;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Invoicing.EventHandlers
{
    public partial class PaymentCompletedHandler(
        IInvoiceRepository invoiceRepository,
        ILogger<PaymentCompletedHandler> logger) : INotificationHandler<DomainEventNotification<PaymentCompletedEvent>>
    {
        public async Task Handle(DomainEventNotification<PaymentCompletedEvent> notification,
            CancellationToken cancellationToken)
        {
            PaymentCompletedEvent domainEvent = notification.DomainEvent;
            LogProcessing(logger, domainEvent.PaymentId, domainEvent.Allocations.Count);

            foreach (PaymentAllocationEventData allocation in domainEvent.Allocations)
            {
                Invoice? invoice = await invoiceRepository.GetByIdAsync(allocation.InvoiceId, cancellationToken);
                if (invoice == null)
                {
                    LogInvoiceNotFound(logger, allocation.InvoiceId, domainEvent.PaymentId);
                    continue;
                }

                LogApplyingPayment(logger, allocation.Amount.Amount, allocation.Amount.Currency, invoice.Id, domainEvent.PaymentId);

                invoice.ApplyPayment(domainEvent.PaymentId, allocation.Amount);
                await invoiceRepository.UpdateAsync(invoice, cancellationToken);
            }
        }

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Processing PaymentCompletedEvent for Payment {PaymentId} with {AllocationCount} allocations")]
        private static partial void LogProcessing(ILogger logger, Guid paymentId, int allocationCount);

        [LoggerMessage(Level = LogLevel.Warning,
            Message = "Invoice {InvoiceId} not found for allocation from Payment {PaymentId}")]
        private static partial void LogInvoiceNotFound(ILogger logger, Guid invoiceId, Guid paymentId);

        [LoggerMessage(Level = LogLevel.Information,
            Message = "Applying {Amount} {Currency} to Invoice {InvoiceId} from Payment {PaymentId}")]
        private static partial void LogApplyingPayment(ILogger logger, decimal amount, string currency, Guid invoiceId, Guid paymentId);
    }
}
