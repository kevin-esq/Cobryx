using System.Linq.Expressions;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;

namespace Cobryx.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    // These will be implemented properly with EF Core
    public Task<Customer?> GetByIdAsync(Guid id) => Task.FromResult<Customer?>(null);
    public Task<IEnumerable<Customer>> GetAllAsync() => Task.FromResult<IEnumerable<Customer>>(Enumerable.Empty<Customer>());
    public Task<IEnumerable<Customer>> FindAsync(Expression<Func<Customer, bool>> predicate) => Task.FromResult<IEnumerable<Customer>>(Enumerable.Empty<Customer>());
    public Task AddAsync(Customer entity) => Task.CompletedTask;
    public Task UpdateAsync(Customer entity) => Task.CompletedTask;
    public Task DeleteAsync(Guid id) => Task.CompletedTask;

    public Task<Customer?> GetByPhoneAsync(Guid tenantId, string phone) => Task.FromResult<Customer?>(null);
    public Task<IEnumerable<Customer>> GetByTenantAsync(Guid tenantId) => Task.FromResult<IEnumerable<Customer>>(Enumerable.Empty<Customer>());
}
