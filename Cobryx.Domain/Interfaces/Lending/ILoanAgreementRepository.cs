using Cobryx.Domain.Entities.Lending;

namespace Cobryx.Domain.Interfaces.Lending;

public interface ILoanAgreementRepository
{
    Task<LoanAgreement?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(LoanAgreement agreement, CancellationToken ct = default);
    Task UpdateAsync(LoanAgreement agreement, CancellationToken ct = default);
}
