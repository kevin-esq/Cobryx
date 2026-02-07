using Cobryx.Domain.Entities.Invoicing;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Payments.Commands.ProcessPayment;
using Cobryx.Domain.Entities;
using Concordia;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cobryx.IntegrationTests;

public class InvoicingFlowTests : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly CobryxWebApplicationFactory _factory;

    public InvoicingFlowTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProcessPayment_ShouldMarkInvoiceAsPaid_ThroughDecoupledEventFlow()
    {
        using var scope = _factory.Services.CreateScope();
        var invoiceRepo = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var customerRepo = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        var paymentMethodRepo = scope.ServiceProvider.GetRequiredService<IPaymentMethodRepository>();
        var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
        var dbContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var tenantId = tenantProvider.GetTenantId().GetValueOrDefault();

        var customer = new Customer(tenantId, "Test", "User", "5551234", null, null);
        await customerRepo.AddAsync(customer);

        var paymentMethod = new PaymentMethod(tenantId, "Cash", "CASH");
        await paymentMethodRepo.AddAsync(paymentMethod);

        var invoice = new Invoice(tenantId, customer.Id, "TEST-INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        invoice.AddItem("Production Item", 1, 500, 0, false);
        invoice.Issue();
        await invoiceRepo.AddAsync(invoice);

        await dbContext.SaveChangesAsync();

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var command = new ProcessPaymentCommand(
            CustomerId: customer.Id,
            PaymentMethodId: paymentMethod.Id,
            Amount: 500,
            Currency: "MXN",
            PaymentDate: DateTime.UtcNow,
            InvoiceIds: new List<Guid> { invoice.Id }
        );

        var result = await sender.Send(command);

        result.IsSuccess.Should().BeTrue();
        var paymentId = result.Value;

        var updatedInvoice = await invoiceRepo.GetByIdAsync(invoice.Id);
        updatedInvoice.Should().NotBeNull();
        updatedInvoice!.Status.Should().Be(InvoiceStatus.Paid);
        updatedInvoice.TotalPaid.Amount.Should().Be(500);

        var paymentRepo = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var payment = await paymentRepo.GetByIdAsync(paymentId);
        payment.Should().NotBeNull();
        payment!.Status.Should().Be(PaymentStatus.Completed);
    }
}
