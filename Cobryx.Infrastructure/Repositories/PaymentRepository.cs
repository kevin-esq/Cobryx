using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(CobryxDbContext dbContext) : base(dbContext) { }

    public async Task<IEnumerable<Payment>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.CustomerId == customerId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Reference == reference, cancellationToken);
    }
}
