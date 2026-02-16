using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Professional summary of a commercial invoice.
/// </summary>
public record InvoiceSummaryContract(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string InvoiceNumber,
    DateTime DueDate,
    decimal TotalAmount,
    string Currency,
    string Status
)
{
    /// <summary>Unique identifier for the invoice.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [Required]
    public Guid Id { get; init; } = Id;

    /// <summary>Identifier of the billed customer.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa7</example>
    [Required]
    public Guid CustomerId { get; init; } = CustomerId;

    /// <summary>Full name of the billed customer.</summary>
    /// <example>Jane Smith</example>
    [Required]
    public string CustomerName { get; init; } = CustomerName;

    /// <summary>Internal reference or serial number.</summary>
    /// <example>INV-2026-00452</example>
    [Required]
    public string InvoiceNumber { get; init; } = InvoiceNumber;

    /// <summary>The date when the invoice becomes overdue.</summary>
    /// <example>2026-03-31T23:59:59Z</example>
    [Required]
    public DateTime DueDate { get; init; } = DueDate;

    /// <summary>Total amount due including taxes.</summary>
    /// <example>1740.00</example>
    [Required]
    public decimal TotalAmount { get; init; } = TotalAmount;

    /// <summary>Currency code (ISO 4217).</summary>
    /// <example>MXN</example>
    [Required]
    public string Currency { get; init; } = Currency;

    /// <summary>Operational status of the invoice.</summary>
    /// <remarks>Valid values: PENDING, PAID, OVERDUE, VOID</remarks>
    /// <example>PENDING</example>
    [Required]
    public string Status { get; init; } = Status;
}
