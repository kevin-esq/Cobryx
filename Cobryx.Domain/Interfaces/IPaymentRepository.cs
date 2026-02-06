using Cobryx.Domain.Entities.Payments;

namespace Cobryx.Domain.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IEnumerable<Payment>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
