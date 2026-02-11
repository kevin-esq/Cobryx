using Cobryx.Domain.DomainServices.Lending;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Lending.Enums;

namespace Cobryx.Domain.Tests.Lending;

public class AmortizationServiceTests
{
    private readonly AmortizationService _service = new();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();

    [Fact]
    public void GenerateSchedule_CreatesCorrectNumberOfInstallments()
    {
        var agreement = CreateAgreement(principal: 10000, installments: 10);
        var policy = CreateExplicitInterestPolicy(rate: 5);

        var schedule = _service.GenerateSchedule(agreement, policy);

        Assert.Equal(10, schedule.Count);
    }

    [Fact]
    public void GenerateSchedule_PrincipalSumsToTotal()
    {
        var agreement = CreateAgreement(principal: 10000, installments: 4);
        var policy = CreateExplicitInterestPolicy(rate: 5);

        var schedule = _service.GenerateSchedule(agreement, policy);

        var totalPrincipal = schedule.Sum(i => i.PrincipalAmount);
        Assert.Equal(10000m, totalPrincipal);
    }

    [Fact]
    public void GenerateSchedule_SimpleInterest_CalculatesCorrectTotalInterest()
    {
        var agreement = CreateAgreement(principal: 10000, installments: 4);
        var policy = CreateExplicitInterestPolicy(rate: 10);

        var schedule = _service.GenerateSchedule(agreement, policy);

        var totalInterest = schedule.Sum(i => i.InterestAmount);
        Assert.Equal(4000m, totalInterest);
    }

    [Fact]
    public void GenerateSchedule_InstallmentsHaveIncrementingDueDates()
    {
        var startDate = new DateTime(2025, 1, 1);
        var agreement = CreateAgreement(principal: 10000, installments: 4, startDate: startDate);
        var policy = CreateExplicitInterestPolicy(rate: 5);

        var schedule = _service.GenerateSchedule(agreement, policy);

        Assert.Equal(new DateTime(2025, 1, 8), schedule[0].DueDate);
        Assert.Equal(new DateTime(2025, 1, 15), schedule[1].DueDate);
        Assert.Equal(new DateTime(2025, 1, 22), schedule[2].DueDate);
        Assert.Equal(new DateTime(2025, 1, 29), schedule[3].DueDate);
    }

    [Fact]
    public void GenerateSchedule_AllInstallmentsStartAsPending()
    {
        var agreement = CreateAgreement(principal: 10000, installments: 4);
        var policy = CreateExplicitInterestPolicy(rate: 5);

        var schedule = _service.GenerateSchedule(agreement, policy);

        Assert.All(schedule, i => Assert.Equal(InstallmentStatus.Pending, i.Status));
    }

    [Fact]
    public void GenerateSchedule_ZeroInterest_OnlyPrincipal()
    {
        var agreement = CreateAgreement(principal: 10000, installments: 4);
        var policy = CreateExplicitInterestPolicy(rate: 0);

        var schedule = _service.GenerateSchedule(agreement, policy);

        Assert.All(schedule, i => Assert.Equal(0m, i.InterestAmount));
        Assert.Equal(10000m, schedule.Sum(i => i.PrincipalAmount));
    }

    private LoanAgreement CreateAgreement(
        decimal principal,
        int installments,
        DateTime? startDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.Date;
        var interestPolicyId = Guid.NewGuid();

        return new LoanAgreement(
            tenantId: _tenantId,
            customerId: _customerId,
            principalAmount: principal,
            interestPolicyId: interestPolicyId,
            paymentFrequency: PaymentFrequency.Weekly,
            numberOfInstallments: installments,
            startDate: start,
            firstPaymentDate: start.AddDays(7),
            origin: LoanOrigin.CashLoan,
            roundingMode: RoundingMode.None);
    }

    private InterestPolicy CreateExplicitInterestPolicy(decimal rate)
    {
        return InterestPolicy.CreateExplicit(
            tenantId: _tenantId,
            name: "Test Policy",
            code: "TEST-INTEREST",
            rate: rate,
            method: InterestMethod.Simple);
    }
}
