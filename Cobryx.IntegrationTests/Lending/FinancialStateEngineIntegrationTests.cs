using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Lending.Enums;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Entities.Accounting;
using Cobryx.Domain.Enums;
using Cobryx.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cobryx.IntegrationTests.Lending;

[Collection("Sequential")]
public class FinancialStateEngineIntegrationTests : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly CobryxWebApplicationFactory _factory;

    public FinancialStateEngineIntegrationTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldRecordAuditTrailOnTransition()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        var stateEngine = scope.ServiceProvider.GetRequiredService<FinancialStateEngine>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var tenantId = Guid.NewGuid();
        var (customerId, agreementId) = await SeedBaseDataAsync(context, tenantId);

        var loan = new Loan(tenantId, customerId, agreementId, "L-INTEG-1", 1000);

        // Add an overdue installment (10 days past due)
        var dueDate = clock.UtcNow.AddDays(-10);
        var installment = new Installment(loan.Id, 1, dueDate, 500, 50);
        loan.AddInstallments(new[] { installment });

        context.Loans.Add(loan);
        await context.SaveChangesAsync();

        // Act
        await stateEngine.UpdateStatusAsync(loan.Id, "Integ Test Trigger");

        // Assert
        var updatedLoan = await context.Loans.FirstAsync(l => l.Id == loan.Id);
        updatedLoan.FinancialStatus.Should().Be(FinancialStatus.Late);
        updatedLoan.FinancialDaysPastDue.Should().Be(10);

        var audit = await context.FinancialStatusAudits
            .Where(a => a.LoanId == loan.Id)
            .FirstOrDefaultAsync();

        audit.Should().NotBeNull();
        audit!.NewStatus.Should().Be(FinancialStatus.Late);
        audit.Reason.Should().Be("Integ Test Trigger");
    }

    [Fact]
    public async Task ExecuteChargeOffAsync_ShouldPerformAccountingAndStateTransition()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        var stateEngine = scope.ServiceProvider.GetRequiredService<FinancialStateEngine>();

        var tenantId = Guid.NewGuid();
        var (customerId, agreementId) = await SeedBaseDataAsync(context, tenantId);

        var loan = new Loan(tenantId, customerId, agreementId, "L-INTEG-CO", 1000);
        context.Loans.Add(loan);
        await context.SaveChangesAsync();

        // Act
        await stateEngine.ExecuteChargeOffAsync(loan.Id, "Loss Verification");

        // Assert
        var updatedLoan = await context.Loans.FirstAsync(l => l.Id == loan.Id);
        updatedLoan.FinancialStatus.Should().Be(FinancialStatus.ChargedOff);
        updatedLoan.Status.Should().Be(LoanStatus.Closed);

        // Verify Ledger (One side: Loan Principal Credit, Other side: Loss/Expense Debit)
        var tx = await context.LedgerTransactions
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Description.Contains("CHARGE-OFF") && t.Description.Contains(loan.LoanNumber));

        tx.Should().NotBeNull();
        tx!.Entries.Should().HaveCount(2);
        tx.IsPosted.Should().BeTrue();
    }

    private async Task<(Guid CustomerId, Guid AgreementId)> SeedBaseDataAsync(CobryxDbContext context, Guid tenantId)
    {
        // 1. Tenant
        var tenant = new Tenant("Test Bank", "bank@test.com");
        typeof(Tenant).GetProperty("Id")!.SetValue(tenant, tenantId);
        context.Tenants.Add(tenant);

        // 2. System Accounts (Required by PostingEngine)
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

        // 3. Customer
        var customerId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Test", "User", "123456789", "test@user.com", null, null);
        typeof(Customer).GetProperty("Id")!.SetValue(customer, customerId);
        context.Customers.Add(customer);

        // 4. Agreement
        var agreementId = Guid.NewGuid();
        var agreement = new LoanAgreement(
            tenantId, customerId, 1000, Guid.NewGuid(), Cobryx.Domain.Entities.Lending.Enums.PaymentFrequency.Monthly, 12,
            DateTime.UtcNow, DateTime.UtcNow.AddMonths(1), LoanOrigin.CashLoan);
        typeof(LoanAgreement).GetProperty("Id")!.SetValue(agreement, agreementId);
        context.LoanAgreements.Add(agreement);

        await context.SaveChangesAsync();
        return (customerId, agreementId);
    }
}
