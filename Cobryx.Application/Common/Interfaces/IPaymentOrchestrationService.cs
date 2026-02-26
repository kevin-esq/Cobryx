using Cobryx.Domain.Common;
using Concordia;

namespace Cobryx.Application.Common.Interfaces;

public interface IPaymentOrchestrationService
{
    /// <summary>
    /// Processes a payment failure and decides on the optimal recovery strategy (Auto-Charge vs. Manual Link).
    /// </summary>
    Task<Result> HandlePaymentFailureAsync(
        Guid customerId,
        string? stripeFailureCode,
        decimal amount,
        string currency,
        string description,
        string? stripePaymentIntentId = null,
        Guid? paymentLinkId = null,
        CancellationToken ct = default);
}
