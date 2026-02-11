namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Public contract for registering a new payment method (e.g. Bank Account, Cash).
/// </summary>
public record CreatePaymentMethodRequest(
    string Name,
    string Code,
    string? Description = null
);
