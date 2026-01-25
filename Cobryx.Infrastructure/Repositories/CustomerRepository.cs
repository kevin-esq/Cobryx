using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(CobryxDbContext dbContext) : base(dbContext) { }

    public async Task<Customer?> GetByPhoneAsync(Guid tenantId, string phone)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.Phone == phone); // TenantId handled by Global Filter
    }

    public async Task<IEnumerable<Customer>> GetByTenantAsync(Guid tenantId)
    {
        return await _dbSet.ToListAsync();
    }
}
