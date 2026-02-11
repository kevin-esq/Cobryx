using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> SearchByNameAsync(Guid tenantId, string searchTerm, CancellationToken cancellationToken = default);
}
