namespace Cobryx.Api.Contracts.V1.Lending;

/// <summary>
/// Public contract for registering a payment against a loan.
/// </summary>
public record LoanPaymentRequest(
    decimal Amount,
    Guid PaymentMethodId,
    DateTime PaidAt,
    string? Reference = null,
    string? Notes = null
);
