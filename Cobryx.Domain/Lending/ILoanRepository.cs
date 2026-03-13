namespace Cobryx.Domain.Lending;

public interface ILoanRepository
{
    public Task<Loan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    public Task<Loan?> GetByIdWithInstallmentsAsync(Guid id, CancellationToken ct = default);
    public Task<IReadOnlyList<Loan>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    public Task<IReadOnlyList<Loan>> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default);
    public Task AddAsync(Loan loan, CancellationToken ct = default);
    public Task UpdateAsync(Loan loan, CancellationToken ct = default);
}
