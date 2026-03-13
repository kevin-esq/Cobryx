using Cobryx.Domain.Payments;


namespace Cobryx.Domain.Interfaces;

public interface IPaymentMethodRepository : IRepository<PaymentMethod>
{
    public Task<PaymentMethod?> GetByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default);
    public Task<IReadOnlyList<PaymentMethod>> GetAllActiveAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
