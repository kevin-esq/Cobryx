using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByPhoneAsync(Guid tenantId, string phone);
    Task<IEnumerable<Customer>> GetByTenantAsync(Guid tenantId);
}
