namespace Cobryx.Domain.Lending;

public interface ICreditSaleRepository
{
    public Task<CreditSale?> GetByIdAsync(Guid id, CancellationToken ct = default);
    public Task<IReadOnlyList<CreditSale>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    public Task AddAsync(CreditSale creditSale, CancellationToken ct = default);
    public Task UpdateAsync(CreditSale creditSale, CancellationToken ct = default);
}
