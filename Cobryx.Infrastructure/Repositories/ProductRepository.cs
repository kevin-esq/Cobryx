using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class ProductRepository(CobryxDbContext dbContext) : BaseRepository<Product>(dbContext), IProductRepository
{
    public async Task<IEnumerable<Product>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default) => await _dbSet.ToListAsync(cancellationToken);

    public async Task<Product?> GetBySkuAsync(Guid tenantId, string sku, CancellationToken cancellationToken = default)
    {
        _ = tenantId;
        return await _dbSet.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(Guid tenantId, string searchTerm, CancellationToken cancellationToken = default)
    {
        _ = tenantId;
        return await _dbSet
            .Where(p => p.Name.Contains(searchTerm))
            .ToListAsync(cancellationToken);
    }
}
