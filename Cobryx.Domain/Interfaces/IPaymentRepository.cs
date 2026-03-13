using Cobryx.Domain.Payments;

namespace Cobryx.Domain.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    public Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken cancellationToken = default);
    public Task<Payment?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);
}
