using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;

namespace Cobryx.Domain.Tests.Lending;

public class PaymentApplicationServiceTests
{
    private readonly PaymentApplicationService _service = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public void Apply_LateFees_AreAppliedFirst()
    {
        var loan = CreateLoanWithLateFees(lateFeeBalance: 100m);
        var policy = CreateStandardPolicy();

        var allocations = _service.Apply(loan, 50m, policy);

        Assert.Single(allocations);
        Assert.Equal(PaymentApplicationType.LateFees, allocations[0].Type);
        Assert.Equal(50m, allocations[0].Amount);
    }

    [Fact]
    public void Apply_InterestBeforePrincipal()
    {
        var loan = CreateLoanWithInstallments(principal: 1000m, interest: 100m);
        var policy = CreateStandardPolicy();

        var allocations = _service.Apply(loan, 150m, policy);

        var interestAllocation = allocations.First(a => a.Type == PaymentApplicationType.Interest);
        var principalAllocation = allocations.First(a => a.Type == PaymentApplicationType.Principal);

        Assert.Equal(100m, interestAllocation.Amount);
        Assert.Equal(50m, principalAllocation.Amount);
    }

    [Fact]
    public void Apply_PartialPayment_LeavesRemainingBalance()
    {
        var loan = CreateLoanWithInstallments(principal: 1000m, interest: 100m);
        var policy = CreateStandardPolicy();

        var allocations = _service.Apply(loan, 50m, policy);

        Assert.Single(allocations);
        Assert.Equal(PaymentApplicationType.Interest, allocations[0].Type);
        Assert.Equal(50m, allocations[0].Amount);
    }

    [Fact]
    public void Apply_Overpayment_CreatesExcessAllocation()
    {
        var loan = CreateLoanWithInstallments(principal: 1000m, interest: 0m);
        var policy = CreateStandardPolicy();

        var allocations = _service.Apply(loan, 1500m, policy);

        var principalAllocations = allocations.Where(a => a.Type == PaymentApplicationType.Principal).Sum(a => a.Amount);
        Assert.Equal(1500m, principalAllocations);
    }

    [Fact]
    public void Apply_ExactPayment_PaysOffInstallment()
    {
        var loan = CreateLoanWithInstallments(principal: 500m, interest: 50m);
        var policy = CreateStandardPolicy();

        var allocations = _service.Apply(loan, 550m, policy);

        var interest = allocations.Where(a => a.Type == PaymentApplicationType.Interest).Sum(a => a.Amount);
        var principal = allocations.Where(a => a.Type == PaymentApplicationType.Principal).Sum(a => a.Amount);

        Assert.Equal(50m, interest);
        Assert.Equal(500m, principal);
    }

    [Fact]
    public void Apply_AllocationContainsInstallmentId()
    {
        var loan = CreateLoanWithInstallments(principal: 1000m, interest: 100m);
        var policy = CreateStandardPolicy();

        var allocations = _service.Apply(loan, 200m, policy);

        var installmentAllocations = allocations.Where(a => a.InstallmentId.HasValue);
        Assert.NotEmpty(installmentAllocations);
    }

    private Loan CreateLoanWithLateFees(decimal lateFeeBalance)
    {
        var loan = new Loan(_tenantId, Guid.NewGuid(), Guid.NewGuid(), "TEST-001", 10000m);
        loan.AssessLateFees(DateTime.UtcNow, lateFeeBalance);
        return loan;
    }

    private Loan CreateLoanWithInstallments(decimal principal, decimal interest)
    {
        var loan = new Loan(_tenantId, Guid.NewGuid(), Guid.NewGuid(), "TEST-001", principal);

        var installment = new Installment(
            loanId: loan.Id,
            installmentNumber: 1,
            dueDate: DateTime.UtcNow.AddDays(7),
            principalAmount: principal,
            interestAmount: interest);

        loan.AddInstallments([installment]);
        return loan;
    }

    private PaymentApplicationPolicy CreateStandardPolicy() => PaymentApplicationPolicy.CreateStandard(_tenantId, "Standard", "STD-PAYMENT");
}
