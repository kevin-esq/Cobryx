using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Lending.Enums;
using Cobryx.Domain.Interfaces.Lending;

namespace Cobryx.Domain.DomainServices.Lending;

/// <summary>
/// Service responsible for generating amortization schedules based on loan agreement terms.
/// </summary>
public class AmortizationService : IAmortizationService
{
    /// <summary>
    /// Generates an amortization schedule for a given loan agreement and interest policy.
    /// </summary>
    /// <param name="agreement">The loan agreement containing principal amount, number of installments, and payment dates.</param>
    /// <param name="interestPolicy">The interest policy to calculate total interest for the loan.</param>
    /// <returns>A read-only list of <see cref="Installment"/> objects representing the amortization schedule.</returns>
    public IReadOnlyList<Installment> GenerateSchedule(LoanAgreement agreement, InterestPolicy interestPolicy)
    {
        var installments = new List<Installment>();
        var daysBetweenPayments = agreement.GetDaysBetweenPayments();
        var totalInterest = interestPolicy.CalculateInterest(agreement.PrincipalAmount, agreement.NumberOfInstallments);
        var principalPerInstallment = agreement.PrincipalAmount / agreement.NumberOfInstallments;
        var interestPerInstallment = totalInterest / agreement.NumberOfInstallments;

        principalPerInstallment = ApplyRounding(principalPerInstallment, agreement.RoundingMode);
        interestPerInstallment = ApplyRounding(interestPerInstallment, agreement.RoundingMode);

        var currentDueDate = agreement.FirstPaymentDate;
        var remainingPrincipal = agreement.PrincipalAmount;
        var remainingInterest = totalInterest;

        for (int i = 1; i <= agreement.NumberOfInstallments; i++)
        {
            var isLastInstallment = i == agreement.NumberOfInstallments;

            var principal = isLastInstallment ? remainingPrincipal : principalPerInstallment;
            var interest = isLastInstallment ? remainingInterest : interestPerInstallment;

            var installment = new Installment(
                loanId: Guid.Empty,
                installmentNumber: i,
                dueDate: currentDueDate,
                principalAmount: principal,
                interestAmount: interest);

            installments.Add(installment);

            remainingPrincipal -= principal;
            remainingInterest -= interest;
            currentDueDate = currentDueDate.AddDays(daysBetweenPayments);
        }

        return installments;
    }

    /// <summary>
    /// Applies a specified rounding mode to a decimal amount.
    /// </summary>
    /// <param name="amount">The decimal amount to be rounded.</param>
    /// <param name="mode">The <see cref="RoundingMode"/> to apply.</param>
    /// <returns>The rounded decimal amount.</returns>
    private static decimal ApplyRounding(decimal amount, RoundingMode mode)
    {
        return mode switch
        {
            RoundingMode.None => amount,
            RoundingMode.ToNearest => Math.Round(amount, 0),
            RoundingMode.ToTen => Math.Round(amount / 10) * 10,
            RoundingMode.ToFifty => Math.Round(amount / 50) * 50,
            RoundingMode.ToHundred => Math.Round(amount / 100) * 100,
            _ => amount
        };
    }
}
