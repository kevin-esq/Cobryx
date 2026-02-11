using Cobryx.Domain.Entities.Lending;

namespace Cobryx.Domain.Interfaces.Lending;

public interface ICreditSaleRepository
{
    Task<CreditSale?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CreditSale>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(CreditSale creditSale, CancellationToken ct = default);
    Task UpdateAsync(CreditSale creditSale, CancellationToken ct = default);
}
