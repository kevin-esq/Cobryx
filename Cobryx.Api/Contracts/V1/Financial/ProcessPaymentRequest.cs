namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Public contract for processing a payment and allocating it to invoices.
/// </summary>
public record ProcessPaymentRequest(
    Guid CustomerId,
    Guid PaymentMethodId,
    decimal Amount,
    string Currency,
    DateTime PaymentDate,
    string? Reference = null,
    string? Notes = null,
    List<Guid>? InvoiceIds = null
)
{
    /// <summary>Unique identifier for the customer making the payment.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CustomerId { get; init; } = CustomerId;

    /// <summary>The payment method used (Bank Transfer, Cash, Stripe).</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa7</example>
    public Guid PaymentMethodId { get; init; } = PaymentMethodId;

    /// <summary>The total amount paid.</summary>
    /// <example>1500.50</example>
    public decimal Amount { get; init; } = Amount;

    /// <summary>The currency of the payment (ISO 4217).</summary>
    /// <example>MXN</example>
    public string Currency { get; init; } = Currency;

    /// <summary>The date the payment was received.</summary>
    /// <example>2026-02-16T14:34:01Z</example>
    public DateTime PaymentDate { get; init; } = PaymentDate;

    /// <summary>Internal reference number or bank folio.</summary>
    /// <example>REF-88421</example>
    public string? Reference { get; init; } = Reference;

    /// <summary>Optional internal notes about the transaction.</summary>
    /// <example>Payment for January invoices.</example>
    public string? Notes { get; init; } = Notes;

    /// <summary>Optional list of specific invoice IDs to allocate this payment against.</summary>
    public List<Guid>? InvoiceIds { get; init; } = InvoiceIds;
}
