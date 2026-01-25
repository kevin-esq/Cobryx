using System.Linq.Expressions;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;

namespace Cobryx.Infrastructure.Repositories;

public class CreditRepository : ICreditRepository
{
    public Task<Credit?> GetByIdAsync(Guid id) => Task.FromResult<Credit?>(null);
    public Task<IEnumerable<Credit>> GetAllAsync() => Task.FromResult<IEnumerable<Credit>>(Enumerable.Empty<Credit>());
    public Task<IEnumerable<Credit>> FindAsync(Expression<Func<Credit, bool>> predicate) => Task.FromResult<IEnumerable<Credit>>(Enumerable.Empty<Credit>());
    public Task AddAsync(Credit entity) => Task.CompletedTask;
    public Task UpdateAsync(Credit entity) => Task.CompletedTask;
    public Task DeleteAsync(Guid id) => Task.CompletedTask;

    public Task<IEnumerable<Credit>> GetByCustomerAsync(Guid customerId) => Task.FromResult<IEnumerable<Credit>>(Enumerable.Empty<Credit>());
    public Task<IEnumerable<Credit>> GetByTenantAsync(Guid tenantId) => Task.FromResult<IEnumerable<Credit>>(Enumerable.Empty<Credit>());
}
