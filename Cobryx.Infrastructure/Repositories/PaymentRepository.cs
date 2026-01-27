using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(CobryxDbContext dbContext) : base(dbContext) { }

    public async Task<IEnumerable<Payment>> GetByCreditAsync(Guid creditId)
    {
        return await _dbSet.Where(p => p.CreditId == creditId).ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetByTenantAsync(Guid tenantId)
    {
        return await _dbSet.ToListAsync(); // Query filter handles tenantId
    }
}
