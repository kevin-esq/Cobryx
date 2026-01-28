using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class PaymentMethodRepository : BaseRepository<PaymentMethod>, IPaymentMethodRepository
{
    public PaymentMethodRepository(CobryxDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<PaymentMethod?> GetByCodeAsync(Guid tenantId, string code)
    {
        return await _dbContext.PaymentMethods
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Code == code.ToUpperInvariant());
    }

    public async Task<IReadOnlyList<PaymentMethod>> GetAllActiveAsync(Guid tenantId)
    {
        return await _dbContext.PaymentMethods
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
