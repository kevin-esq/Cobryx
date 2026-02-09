using Cobryx.Domain.Entities.Invoicing;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Enums;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Events.Invoicing;
using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Cobryx.IntegrationTests.Domain;

public class InvoicingDomainTests
{
    [Fact]
    public void Invoice_ShouldEmitIssuedEvent_WhenIssued()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var invoice = new Invoice(tenantId, customerId, "INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        invoice.AddItem("Item 1", 1, 100, 0, false);

        invoice.Issue();

        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceIssuedEvent);
    }

    [Fact]
    public void Invoice_ShouldThrow_WhenIssuingWithNoItems()
    {
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30));

        var act = () => invoice.Issue();

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be(DomainErrorCode.Invoicing.InvoiceNoItems);
    }

    [Fact]
    public void Invoice_ShouldPreventAddingItems_AfterIssuance()
    {
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        invoice.AddItem("Item 1", 1, 100, 0, false);
        invoice.Issue();

        var act = () => invoice.AddItem("Item 2", 1, 50, 0, false);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be(DomainErrorCode.Invoicing.InvoiceNotDraftAddItem);
    }

    [Fact]
    public void Invoice_ShouldTransitionToPaid_WhenFullySettled()
    {
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        invoice.AddItem("Item 1", 1, 100, 0, false);
        invoice.Issue();
        var amount = new Money(100, "MXN");

        invoice.ApplyPayment(Guid.NewGuid(), amount);

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.DomainEvents.Should().ContainSingle(e => e is InvoicePaidEvent);
    }

    [Fact]
    public void Invoice_ShouldBeIdempotent_WhenApplyingSamePaymentTwice()
    {
        var invoice = new Invoice(Guid.NewGuid(), Guid.NewGuid(), "INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        invoice.AddItem("Item 1", 1, 100, 0, false);
        invoice.Issue();
        var paymentId = Guid.NewGuid();
        var amount = new Money(50, "MXN");

        invoice.ApplyPayment(paymentId, amount);
        invoice.ApplyPayment(paymentId, amount);

        invoice.TotalPaid.Amount.Should().Be(50);
        invoice.Status.Should().Be(InvoiceStatus.Partial);
    }

    [Fact]
    public void Payment_ShouldTransitionToProcessing_WhenInitiated()
    {
        var payment = new Payment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Money(100, "MXN"), DateTime.UtcNow);

        payment.Initiate();

        payment.Status.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public void Payment_ShouldEmitCompletedEvent_WhenCompleted()
    {
        var payment = new Payment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Money(100, "MXN"), DateTime.UtcNow);
        payment.Initiate();

        payment.Complete();

        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.DomainEvents.Should().ContainSingle(e => e is PaymentCompletedEvent);
    }

    [Fact]
    public void Payment_ShouldThrow_IfCompletedDirectlyFromPending()
    {
        var payment = new Payment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new Money(100, "MXN"), DateTime.UtcNow);

        var act = () => payment.Complete();

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be(DomainErrorCode.Invoicing.PaymentNotProcessing);
    }
}
