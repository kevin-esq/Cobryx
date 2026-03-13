namespace Cobryx.Domain.Lending;

public interface IInstallmentRepository
{
    public Task<IReadOnlyList<Installment>> GetByLoanIdAsync(Guid loanId, CancellationToken ct = default);
    public Task AddRangeAsync(IEnumerable<Installment> installments, CancellationToken ct = default);
    public Task UpdateAsync(Installment installment, CancellationToken ct = default);
    public Task UpdateRangeAsync(IEnumerable<Installment> installments, CancellationToken ct = default);
}
