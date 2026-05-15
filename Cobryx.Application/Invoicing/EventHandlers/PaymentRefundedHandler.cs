using Cobryx.Application.Common.Events;
using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.ValueObjects;

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
        if (payment == null)
        {
            _logger.LogError("Payment {PaymentId} not found during refund processing.", domainEvent.PaymentId);
            return;
        }

        // FIFO Reversal logic: Reverse the oldest allocations first.
        var activeAllocations = payment.Allocations
            .Where(a => !a.IsReversed)
            .OrderBy(a => a.CreatedAt)
            .ToList();

        decimal remainingToRefund = domainEvent.Amount.Amount;

        foreach (var allocation in activeAllocations)
        {
            if (remainingToRefund <= 0)
                break;

            var amountToReverseInThisAllocation = Math.Min(remainingToRefund, allocation.Amount.Amount);
            var reverseAmount = new Money(amountToReverseInThisAllocation, domainEvent.Amount.Currency);

            var invoice = await _invoiceRepository.GetByIdAsync(allocation.InvoiceId, cancellationToken);
            if (invoice != null)
            {
                _logger.LogInformation("Reversing {Amount} from Invoice {InvoiceId} related to Payment {PaymentId}",
                    reverseAmount, invoice.Id, payment.Id);

                invoice.ReverseAllocation(allocation, reverseAmount);
                await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
            }

            remainingToRefund -= amountToReverseInThisAllocation;
        }

        if (remainingToRefund > 0)
        {
            _logger.LogWarning("Refund amount {RefundAmount} exceeds total active allocations for Payment {PaymentId}. Remaining: {Remaining}",
                domainEvent.Amount.Amount, domainEvent.PaymentId, remainingToRefund);
        }
    }
}
