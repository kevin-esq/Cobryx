using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Payments;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class PaymentRepository(CobryxDbContext dbContext) : BaseRepository<Payment>(dbContext), IPaymentRepository
{

    public async Task<IEnumerable<Payment>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.CustomerId == customerId).ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Reference == paymentIntentId, cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Reference == reference, cancellationToken);
    }
}
