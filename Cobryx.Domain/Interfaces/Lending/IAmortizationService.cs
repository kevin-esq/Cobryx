using Cobryx.Domain.Entities.Lending;

namespace Cobryx.Domain.Interfaces.Lending;

/// <summary>
/// Generates amortization schedules (installments) from loan agreements.
/// </summary>
public interface IAmortizationService
{
    IReadOnlyList<Installment> GenerateSchedule(LoanAgreement agreement, InterestPolicy interestPolicy);
}
