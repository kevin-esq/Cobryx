using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Payments;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class PaymentMethodRepository : BaseRepository<PaymentMethod>, IPaymentMethodRepository
{
    public PaymentMethodRepository(CobryxDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<PaymentMethod?> GetByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentMethods
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Code == code.ToUpperInvariant(), cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentMethod>> GetAllActiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentMethods
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
