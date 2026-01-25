using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(CobryxDbContext dbContext) : base(dbContext) { }

    public async Task<IEnumerable<Product>> GetByTenantAsync(Guid tenantId)
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<Product?> GetBySkuAsync(Guid tenantId, string sku)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Sku == sku);
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(Guid tenantId, string searchTerm)
    {
        return await _dbSet
            .Where(p => p.Name.Contains(searchTerm))
            .ToListAsync();
    }
}
