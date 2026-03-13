using Cobryx.Domain.Lending;

namespace Cobryx.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    public Task<IEnumerable<Product>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    public Task<IEnumerable<Product>> SearchByNameAsync(Guid tenantId, string searchTerm, CancellationToken cancellationToken = default);
}
