using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Application.Payments.Interfaces;

public interface IPaymentGateway
{
    string GatewayName { get; }
    Task<PaymentGatewayResult> ProcessPaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<PaymentGatewayResult> RefundPaymentAsync(Payment payment, Money amount, CancellationToken cancellationToken = default);
}

public record PaymentGatewayResult(
    bool Success,
    string? ExternalTransactionId = null,
    string? ErrorMessage = null,
    Dictionary<string, string>? Metadata = null);
