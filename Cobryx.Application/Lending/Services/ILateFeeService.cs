using Cobryx.Domain.Lending;


namespace Cobryx.Application.Lending.Services;

public interface ILateFeeService
{
    /// <summary>
    /// Calculates and applies a late fee to the loan if applicable according to its policy.
    /// This should be called during daily accrual or collection evaluation.
    /// </summary>
    public decimal AssessLateFee(Loan loan, DateTime date);
}
