namespace Cobryx.Api.Contracts.V1.Lending;

/// <summary>
/// Public contract for registering a payment against a loan.
/// </summary>
public record LoanPaymentRequest(
    decimal Amount,
    string Currency,
    Guid PaymentMethodId,
    DateTime PaidAt,
    string? Reference = null,
    string? Notes = null
)
{
    /// <summary>The amount to be applied to the loan.</summary>
    /// <example>2500.00</example>
    public decimal Amount { get; init; } = Amount;

    /// <summary>Currency code (ISO 4217).</summary>
    /// <example>MXN</example>
    public string Currency { get; init; } = Currency;

    /// <summary>The payment method used (e.g., Bank Transfer, Cash).</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa9</example>
    public Guid PaymentMethodId { get; init; } = PaymentMethodId;

    /// <summary>The precise date and time the payment was made.</summary>
    /// <example>2026-02-16T14:34:01Z</example>
    public DateTime PaidAt { get; init; } = PaidAt;

    /// <summary>Optional reference number or bank folio.</summary>
    /// <example>PAY-91122</example>
    public string? Reference { get; init; } = Reference;

    /// <summary>Optional notes about the payment.</summary>
    /// <example>Payment received via bank transfer.</example>
    public string? Notes { get; init; } = Notes;
}
