using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(CobryxDbContext dbContext) : base(dbContext) { }

    public async Task<IEnumerable<Payment>> GetByCustomerAsync(Guid customerId)
    {
        return await _dbSet.Where(p => p.CustomerId == customerId).ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetByTenantAsync(Guid tenantId)
    {
        return await _dbSet.ToListAsync();
    }
}
