using System.Linq.Expressions;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;

namespace Cobryx.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id) => Task.FromResult<Product?>(null);
    public Task<IEnumerable<Product>> GetAllAsync() => Task.FromResult<IEnumerable<Product>>(Enumerable.Empty<Product>());
    public Task<IEnumerable<Product>> FindAsync(Expression<Func<Product, bool>> predicate) => Task.FromResult<IEnumerable<Product>>(Enumerable.Empty<Product>());
    public Task AddAsync(Product entity) => Task.CompletedTask;
    public Task UpdateAsync(Product entity) => Task.CompletedTask;
    public Task DeleteAsync(Guid id) => Task.CompletedTask;

    public Task<IEnumerable<Product>> GetByTenantAsync(Guid tenantId) => Task.FromResult<IEnumerable<Product>>(Enumerable.Empty<Product>());
    public Task<IEnumerable<Product>> SearchByNameAsync(Guid tenantId, string searchTerm) => Task.FromResult<IEnumerable<Product>>(Enumerable.Empty<Product>());
}
