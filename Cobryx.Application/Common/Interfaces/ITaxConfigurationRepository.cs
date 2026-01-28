using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;

namespace Cobryx.Application.Common.Interfaces;

public interface ITaxConfigurationRepository : IRepository<TaxConfiguration>
{
    Task<TaxConfiguration?> GetDefaultAsync(Guid tenantId);
    Task<IReadOnlyList<TaxConfiguration>> GetAllActiveAsync(Guid tenantId);
}
