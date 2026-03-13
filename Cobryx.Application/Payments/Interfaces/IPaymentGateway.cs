using Cobryx.Domain.Payments;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Application.Payments.Interfaces;

public interface IPaymentGateway
{
    public string GatewayName { get; }
    public Task<PaymentGatewayResult> ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    public Task<PaymentGatewayResult> RefundPaymentAsync(Payment payment, Money amount, CancellationToken cancellationToken = default);
}

public record PaymentGatewayResult(
    bool Success,
    string? ExternalTransactionId = null,
    string? ErrorMessage = null,
    Dictionary<string, string>? Metadata = null);
