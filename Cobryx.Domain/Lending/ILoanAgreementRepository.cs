namespace Cobryx.Domain.Lending;

public interface ILoanAgreementRepository
{
    public Task<LoanAgreement?> GetByIdAsync(Guid id, CancellationToken ct = default);
    public Task AddAsync(LoanAgreement agreement, CancellationToken ct = default);
    public Task UpdateAsync(LoanAgreement agreement, CancellationToken ct = default);
}
