using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class CreditRepository : BaseRepository<Credit>, ICreditRepository
{
    public CreditRepository(CobryxDbContext dbContext) : base(dbContext) { }

    public async Task<IEnumerable<Credit>> GetByCustomerAsync(Guid customerId)
    {
        return await _dbSet
            .Where(c => c.CustomerId == customerId)
            .Include(c => c.Installments)
            .ToListAsync();
    }

    public async Task<IEnumerable<Credit>> GetByTenantAsync(Guid tenantId)
    {
        return await _dbSet.ToListAsync(); // Filtered by Global Query Filter
    }

    public override async Task<Credit?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(c => (ICollection<Installment>)c.Installments) // Need to check cast if private
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}
