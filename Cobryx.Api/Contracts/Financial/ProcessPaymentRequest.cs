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
);
