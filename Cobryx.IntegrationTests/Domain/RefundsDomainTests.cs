using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Events.Invoicing;
using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.ValueObjects;

using FluentAssertions;

namespace Cobryx.IntegrationTests.Domain;

public class RefundsDomainTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _paymentMethodId = Guid.NewGuid();

    [Fact]
    public void Refund_ShouldUpdateRefundedAmountAndEmitEvent()
    {
        var amount = new Money(100, "MXN");
        var payment = new Payment(_tenantId, _customerId, _paymentMethodId, amount, DateTime.UtcNow);
        payment.Initiate();
        payment.Complete();

        var refundAmount = new Money(40, "MXN");

        payment.Refund(refundAmount);

        payment.RefundedAmount.Amount.Should().Be(40);
        payment.RefundableAmount.Amount.Should().Be(60);
        payment.IsFullyRefunded.Should().BeFalse();
        payment.DomainEvents.Should().ContainItemsAssignableTo<PaymentRefundedEvent>();
    }

    [Fact]
    public void Chargeback_ShouldSetTerminalStateAndReverseAllocations()
    {
        var amount = new Money(100, "MXN");
        var payment = new Payment(_tenantId, _customerId, _paymentMethodId, amount, DateTime.UtcNow);
        var invoiceId = Guid.NewGuid();
        payment.AddAllocation(invoiceId, amount);
        payment.Initiate();
        payment.Complete();

        payment.Chargeback();

        payment.Status.Should().Be(PaymentStatus.Chargeback);
        payment.RefundedAmount.Amount.Should().Be(100);
        payment.IsFullyRefunded.Should().BeTrue();
        payment.Allocations.Should().AllSatisfy(a => a.IsReversed.Should().BeTrue());
        payment.DomainEvents.Should().ContainItemsAssignableTo<PaymentChargebackedEvent>();
    }

    [Fact]
    public void Invoice_ReverseAllocation_ShouldDecrementPaidAmountAndEmitEvent()
    {
        var invoice = new Invoice(_tenantId, _customerId, "INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        invoice.AddItem("Test Item", 1, 100, 0, false);
        invoice.Issue();

        var paymentId = Guid.NewGuid();
        var amount = new Money(100, "MXN");
        invoice.ApplyPayment(paymentId, amount);
        invoice.Status.Should().Be(InvoiceStatus.Paid);

        var allocation = new PaymentAllocation(paymentId, invoice.Id, amount);

        invoice.ReverseAllocation(allocation);

        invoice.TotalPaid.Amount.Should().Be(0);
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.DomainEvents.Should().ContainItemsAssignableTo<InvoiceStateReversedEvent>();
    }

    [Fact]
    public void Invoice_ReverseAllocation_ShouldBeIdempotentUsingAllocationFlag()
    {
        var invoice = new Invoice(_tenantId, _customerId, "INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        invoice.AddItem("Test Item", 1, 100, 0, false);
        invoice.Issue();

        var paymentId = Guid.NewGuid();
        var amount = new Money(100, "MXN");
        invoice.ApplyPayment(paymentId, amount);

        var allocation = new PaymentAllocation(paymentId, invoice.Id, amount);

        invoice.ReverseAllocation(allocation);
        var firstReversalPaid = invoice.TotalPaid.Amount;

        allocation.MarkAsReversed();
        invoice.ReverseAllocation(allocation);

        invoice.TotalPaid.Amount.Should().Be(firstReversalPaid);
        invoice.TotalPaid.Amount.Should().Be(0);
    }
}
