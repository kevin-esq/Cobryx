using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IEnumerable<Payment>> GetByCreditAsync(Guid creditId);
    Task<IEnumerable<Payment>> GetByTenantAsync(Guid tenantId);
}
