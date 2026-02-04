using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;

namespace Cobryx.Application.Common.Interfaces;

public interface IPaymentMethodRepository : IRepository<PaymentMethod>
{
    Task<PaymentMethod?> GetByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentMethod>> GetAllActiveAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
