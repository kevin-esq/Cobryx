using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IEnumerable<Payment>> GetByCustomerAsync(Guid customerId);
    Task<IEnumerable<Payment>> GetByTenantAsync(Guid tenantId);
}
