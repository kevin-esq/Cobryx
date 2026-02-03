using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class TaxConfigurationRepository : BaseRepository<TaxConfiguration>, ITaxConfigurationRepository
{
    public TaxConfigurationRepository(CobryxDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<TaxConfiguration?> GetDefaultAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaxConfigurations
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.IsDefault && t.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<TaxConfiguration>> GetAllActiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaxConfigurations
            .Where(t => t.TenantId == tenantId && t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }
}
