namespace Cobryx.Domain.Lending;

/// <summary>
/// Generates amortization schedules (installments) from loan agreements.
/// </summary>
public interface IAmortizationService
{
    public IReadOnlyList<Installment> GenerateSchedule(LoanAgreement agreement, InterestPolicy interestPolicy);
}
