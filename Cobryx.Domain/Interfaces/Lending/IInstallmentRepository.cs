using Cobryx.Domain.Entities.Lending;

namespace Cobryx.Domain.Interfaces.Lending;

public interface IInstallmentRepository
{
    Task<IReadOnlyList<Installment>> GetByLoanIdAsync(Guid loanId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Installment> installments, CancellationToken ct = default);
    Task UpdateAsync(Installment installment, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<Installment> installments, CancellationToken ct = default);
}
