using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Payments.Commands.ProcessPayment;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Infrastructure.Persistence;

using Concordia;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Integration.Tests;

public class InvoicingFlowTests(CobryxWebApplicationFactory factory) : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly CobryxWebApplicationFactory _factory = factory;

    [Fact]
    public async Task ProcessPayment_ShouldMarkInvoiceAsPaid_ThroughDecoupledEventFlow()
    {
        using var scope = _factory.Services.CreateScope();
        var invoiceRepo = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var customerRepo = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();
        var paymentMethodRepo = scope.ServiceProvider.GetRequiredService<IPaymentMethodRepository>();
        var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
        var dbContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();

        var tenantId = tenantProvider.GetTenantId().GetValueOrDefault();

        var customer = new Customer(tenantId, "Test", "User", "5551234", "test@example.com", null, null);
        await customerRepo.AddAsync(customer);

        var paymentMethod = new PaymentMethod(tenantId, "Cash", "CASH");
        await paymentMethodRepo.AddAsync(paymentMethod);

        var invoice = new Invoice(tenantId, customer.Id, "TEST-INV-001", DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        invoice.AddItem("Production Item", 1, 500, 0, false);
        invoice.Issue();
        await invoiceRepo.AddAsync(invoice);

        await dbContext.SaveChangesAsync();

        var outboxCountBefore = await db.OutboxMessages.CountAsync();
        Console.WriteLine($"DEBUG: Outbox count before process: {outboxCountBefore}");

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var command = new ProcessPaymentCommand(
            CustomerId: customer.Id,
            PaymentMethodId: paymentMethod.Id,
            Amount: 500,
            Currency: "MXN",
            PaymentDate: DateTime.UtcNow,
            InvoiceIds: [invoice.Id]
        );

        var result = await sender.Send(command);
        result.IsSuccess.Should().BeTrue();
        var outboxCountAfter = await db.OutboxMessages.CountAsync();
        Console.WriteLine($"DEBUG: Outbox count after process: {outboxCountAfter}");

        var outboxProcessor = new Cobryx.Infrastructure.Messaging.ProcessOutboxJob(
            _factory.Services,
            scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Cobryx.Infrastructure.Messaging.ProcessOutboxJob>>());
        await outboxProcessor.RunAsync(CancellationToken.None);

        using var assertionScope = _factory.Services.CreateScope();
        var assertionInvoiceRepo = assertionScope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var finalInvoice = await assertionInvoiceRepo.GetByIdAsync(invoice.Id);

        finalInvoice.Should().NotBeNull();
        finalInvoice!.Status.Should().Be(InvoiceStatus.Paid);
        finalInvoice.TotalPaid.Amount.Should().Be(500);

        var assertionPaymentRepo = assertionScope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var finalPayment = await assertionPaymentRepo.GetByIdAsync(result.Value);

        finalPayment.Should().NotBeNull();
        finalPayment!.Status.Should().Be(PaymentStatus.Completed);
    }
}
