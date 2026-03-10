using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Services;
using Cobryx.Application.Payments.Services;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Entities.Accounting;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Lending.Enums;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Enums;
using Cobryx.Domain.ValueObjects;
using Cobryx.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cobryx.IntegrationTests.Lending;

[Collection("Sequential")]
public class ReconciliationAtomicityTests : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly CobryxWebApplicationFactory _factory;

    public ReconciliationAtomicityTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HandlePaymentSuccess_ShouldRollbackAll_WhenStateUpdateFails()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        var postingEngine = scope.ServiceProvider.GetRequiredService<Application.Accounting.Services.FinancialPostingEngine>();
        var stripeService = scope.ServiceProvider.GetRequiredService<IStripeService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentLinkReconciliationService>>();

        // MOCK State Engine to THROW EXCEPTION (Simulate Crash/Failure)
        var mockStateEngine = new Mock<FinancialStateEngine>(null!, null!, null!, null!);
        mockStateEngine
            .Setup(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("CRASH_SIMULATION"));

        var reconService = new PaymentLinkReconciliationService(context, postingEngine, stripeService, mockStateEngine.Object, logger);

        var tenantId = Guid.NewGuid();
        var (customerId, agreementId) = await SeedBaseDataAsync(context, tenantId);

        var loan = new Loan(tenantId, customerId, agreementId, "L-ATOM-1", 1000);
        context.Loans.Add(loan);

        var paymentIntentId = "pi_crash_123";
        var link = new PaymentLink(tenantId, customerId, new Money(1000, "MXN"), "token_123", DateTime.UtcNow.AddDays(1), "secret_123", loan.Id, "Ref-123");
        link.MarkAsProcessing(paymentIntentId);
        context.PaymentLinks.Add(link);

        await context.SaveChangesAsync();

        var paidAmount = new Money(1000, "MXN");

        // Act
        Func<Task> act = async () => await reconService.HandlePaymentSuccessAsync(paymentIntentId, paidAmount);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("CRASH_SIMULATION");

        // Verify Rollback (Bypass EF Change Tracker to ensure we see DB state)
        var dbLink = await context.PaymentLinks
            .AsNoTracking()
            .FirstAsync(l => l.StripePaymentIntentId == paymentIntentId);

        dbLink.Status.Should().Be(PaymentLinkStatus.Processing, "Status change to 'Paid' should be rolled back to 'Processing'");

        var paymentExists = await context.Payments
            .AsNoTracking()
            .AnyAsync(p => p.Reference == $"LINK-{link.Id}");
        paymentExists.Should().BeFalse("Payment record should be rolled back");

        var ledgerTxExists = await context.LedgerTransactions
            .AsNoTracking()
            .AnyAsync(t => t.ReferenceId == $"PAY-STRIPE-{paymentIntentId}");
        ledgerTxExists.Should().BeFalse("Ledger Transaction should be rolled back");
    }

    [Fact]
    public async Task RecoverStuckLinks_ShouldReconcileSuccessfulIntents()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        var postingEngine = scope.ServiceProvider.GetRequiredService<Application.Accounting.Services.FinancialPostingEngine>();
        var stateEngine = scope.ServiceProvider.GetRequiredService<FinancialStateEngine>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentLinkReconciliationService>>();

        // MOCK Stripe Service to return 'succeeded'
        var mockStripe = new Mock<IStripeService>();
        mockStripe
            .Setup(x => x.GetPaymentIntentStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("succeeded");

        var reconService = new PaymentLinkReconciliationService(context, postingEngine, mockStripe.Object, stateEngine, logger);

        var tenantId = Guid.NewGuid();
        var (customerId, agreementId) = await SeedBaseDataAsync(context, tenantId);

        var loan = new Loan(tenantId, customerId, agreementId, "L-STUCK-1", 500);
        context.Loans.Add(loan);

        var paymentIntentId = "pi_stuck_456";
        var link = new PaymentLink(tenantId, customerId, new Money(500, "MXN"), "token_stuck", DateTime.UtcNow.AddDays(1), "secret_stuck", loan.Id, "Ref-Stuck");

        // MANUALLY set to Processing and make it OLD
        link.MarkAsProcessing(paymentIntentId);
        // Force UpdatedAt to be old (using reflection because it's set by BaseEntity)
        typeof(BaseEntity).GetProperty("UpdatedAt")!.SetValue(link, DateTime.UtcNow.AddHours(-2));

        context.PaymentLinks.Add(link);
        await context.SaveChangesAsync();

        // ACT: Call recovery with a NEGATIVE timeout to force discovery (cutoff will be in the future)
        // This bypasses the need for complex reflection/waiting in a fast test
        await reconService.RecoverStuckProcessingLinksAsync(TimeSpan.FromMinutes(-60));

        // Assert
        var dbLink = await context.PaymentLinks.AsNoTracking().FirstAsync(l => l.Id == link.Id);
        dbLink.Status.Should().Be(PaymentLinkStatus.Paid, "Stuck but successful link should be reconciled to Paid");

        var ledgerTxExists = await context.LedgerTransactions
            .AsNoTracking()
            .AnyAsync(t => t.ReferenceId == $"PAY-STRIPE-{paymentIntentId}");
        ledgerTxExists.Should().BeTrue("Ledger Transaction should be created by recovery logic");
    }

    private async Task<(Guid CustomerId, Guid AgreementId)> SeedBaseDataAsync(CobryxDbContext context, Guid tenantId)
    {
        var tenant = new Tenant("Test Bank", "bank@test.com");
        typeof(Tenant).GetProperty("Id")!.SetValue(tenant, tenantId);
        context.Tenants.Add(tenant);

        var codes = new[] { "1010", "1210", "4010", "4020", "5010", "4030" };
        foreach (var code in codes)
        {
            var role = code switch
            {
                "1010" => LedgerAccountRole.Available,
                "1210" => LedgerAccountRole.Receivable,
                "4020" => LedgerAccountRole.Fees,
                "5010" => LedgerAccountRole.Loss,
                _ => LedgerAccountRole.None
            };
            var acc = new LedgerAccount(tenantId, code, $"Acc {code}", LedgerAccountType.Asset, role, "MXN", true);
            context.LedgerAccounts.Add(acc);
        }

        var customerId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Atom", "User", "123456789", "atom@user.com", null, null);
        typeof(Customer).GetProperty("Id")!.SetValue(customer, customerId);
        context.Customers.Add(customer);

        var agreementId = Guid.NewGuid();
        var agreement = new LoanAgreement(tenantId, customerId, 1000, Guid.NewGuid(), Cobryx.Domain.Entities.Lending.Enums.PaymentFrequency.Monthly, 12, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1), LoanOrigin.CashLoan);
        typeof(LoanAgreement).GetProperty("Id")!.SetValue(agreement, agreementId);
        context.LoanAgreements.Add(agreement);

        var pm = new PaymentMethod(tenantId, "Stripe Payment", "STRIPE");
        context.PaymentMethods.Add(pm);

        await context.SaveChangesAsync();
        return (customerId, agreementId);
    }
}
