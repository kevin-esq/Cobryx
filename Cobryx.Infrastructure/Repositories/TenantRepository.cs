using System.Linq.Expressions;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;

namespace Cobryx.Infrastructure.Repositories;

public class TenantRepository : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid id) => Task.FromResult<Tenant?>(null);
    public Task<IEnumerable<Tenant>> GetAllAsync() => Task.FromResult<IEnumerable<Tenant>>(Enumerable.Empty<Tenant>());
    public Task<IEnumerable<Tenant>> FindAsync(Expression<Func<Tenant, bool>> predicate) => Task.FromResult<IEnumerable<Tenant>>(Enumerable.Empty<Tenant>());
    public Task AddAsync(Tenant entity) => Task.CompletedTask;
    public Task UpdateAsync(Tenant entity) => Task.CompletedTask;
    public Task DeleteAsync(Guid id) => Task.CompletedTask;
}
