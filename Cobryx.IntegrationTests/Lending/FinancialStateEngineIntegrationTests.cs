using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Infrastructure.Persistence;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.IntegrationTests.Lending;

[Collection("Sequential")]
public class FinancialStateEngineIntegrationTests(CobryxWebApplicationFactory factory) : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly CobryxWebApplicationFactory _factory = factory;

    [Fact]
    public async Task UpdateStatusAsync_ShouldRecordAuditTrailOnTransition()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        var stateEngine = scope.ServiceProvider.GetRequiredService<FinancialStateEngine>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var tenantId = Guid.NewGuid();
        var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
        tenantProvider.SetTenantId(tenantId);

        var (customerId, agreementId) = await SeedBaseDataAsync(context, tenantId);

        var loan = new Loan(tenantId, customerId, agreementId, "L-INTEG-1", new Cobryx.Domain.ValueObjects.Money(1000, "MXN"));

        var dueDate = clock.UtcNow.AddDays(-10);
        var currency = "MXN";
        var amount = new Cobryx.Domain.ValueObjects.Money(500, currency);
        var interest = new Cobryx.Domain.ValueObjects.Money(50, currency);
        var balance = new Cobryx.Domain.ValueObjects.Money(550, currency);
        var installment = new Installment(loan.Id, 1, dueDate, amount, interest, balance);
        loan.AddInstallments([installment]);

        context.Loans.Add(loan);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? "No inner info";
            throw new Exception($"FK Failure caught on line 45: {ex.Message}. Inner: {inner}", ex);
        }

        await stateEngine.UpdateStatusAsync(loan.Id, "Integ Test Trigger");

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
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        var stateEngine = scope.ServiceProvider.GetRequiredService<FinancialStateEngine>();

        var tenantId = Guid.NewGuid();
        var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
        tenantProvider.SetTenantId(tenantId);

        var (customerId, agreementId) = await SeedBaseDataAsync(context, tenantId);

        var loan = new Loan(tenantId, customerId, agreementId, "L-INTEG-CO", new Cobryx.Domain.ValueObjects.Money(1000, "MXN"));
        context.Loans.Add(loan);
        await context.SaveChangesAsync();

        await stateEngine.ExecuteChargeOffAsync(loan.Id, "Loss Verification");

        var updatedLoan = await context.Loans.FirstAsync(l => l.Id == loan.Id);
        updatedLoan.FinancialStatus.Should().Be(FinancialStatus.ChargedOff);
        updatedLoan.Status.Should().Be(LoanStatus.Closed);

        var tx = await context.LedgerTransactions
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Description.Contains("CHARGE-OFF") && t.Description.Contains(loan.LoanNumber));

        tx.Should().NotBeNull();
        tx!.Entries.Should().HaveCount(2);
        tx.IsPosted.Should().BeTrue();
    }

    private static async Task<(Guid customerId, Guid agreementId)> SeedBaseDataAsync(CobryxDbContext context, Guid tenantId)
    {
        var tenant = new Tenant("Integ Tenant", "integ@test.com");
        typeof(Tenant).GetProperty("Id")!.SetValue(tenant, tenantId);
        context.Tenants.Add(tenant);

        var interestPolicy = InterestPolicy.CreateExplicit(tenantId, "Standard Interest", "STD-INT", 12.0m);
        context.InterestPolicies.Add(interestPolicy);

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
        var customer = new Customer(tenantId, "Test", "User", "123456789", "test@user.com", null, null);
        typeof(Customer).GetProperty("Id")!.SetValue(customer, customerId);
        context.Customers.Add(customer);

        var applicationPolicy = PaymentApplicationPolicy.CreateStandard(tenantId, "Standard Application", "STD-APP", true);
        context.PaymentApplicationPolicies.Add(applicationPolicy);

        var lateFeePolicy = LateFeePolicy.CreateFixed(tenantId, "Standard Late Fee", 10.0m);
        context.LateFeePolicies.Add(lateFeePolicy);

        await context.SaveChangesAsync();

        var agreementId = Guid.NewGuid();
        var agreement = new LoanAgreement(
            tenantId,
            customerId,
            1000,
            interestPolicy.Id,
            Cobryx.Domain.Lending.Enums.PaymentFrequency.Monthly,
            12,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1),
            Cobryx.Domain.Lending.Enums.LoanOrigin.CashLoan,
            lateFeePolicyId: lateFeePolicy.Id,
            currency: "MXN",
            paymentApplicationPolicyId: applicationPolicy.Id);
        // Force ID via reflection to ensure consistency
        var idProp = typeof(Cobryx.Domain.Shared.BaseEntity).GetProperty("Id");
        idProp!.SetValue(agreement, agreementId);

        context.LoanAgreements.Add(agreement);

        await context.SaveChangesAsync();
        return (customerId, agreementId);
    }
}
