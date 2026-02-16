namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Public contract for creating a new invoice.
/// </summary>
public record CreateInvoiceRequest(
    Guid CustomerId,
    DateTime DueDate,
    List<InvoiceItemRequest> Items,
    string? Notes = null
)
{
    /// <summary>Unique identifier for the customer to be billed.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CustomerId { get; init; } = CustomerId;

    /// <summary>The date when the invoice becomes overdue.</summary>
    /// <example>2026-03-31T23:59:59Z</example>
    public DateTime DueDate { get; init; } = DueDate;

    /// <summary>The collection of line items for the invoice.</summary>
    public List<InvoiceItemRequest> Items { get; init; } = Items;

    /// <summary>Optional notes for the customer.</summary>
    /// <example>Thank you for your business!</example>
    public string? Notes { get; init; } = Notes;
}

/// <summary>
/// Line item for a new invoice.
/// </summary>
public record InvoiceItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    Guid? TaxConfigurationId = null
)
{
    /// <summary>Description of the product or service.</summary>
    /// <example>Professional Consulting Services</example>
    public string Description { get; init; } = Description;

    /// <summary>The number of units billed.</summary>
    /// <example>10.0</example>
    public decimal Quantity { get; init; } = Quantity;

    /// <summary>The price per unit.</summary>
    /// <example>150.00</example>
    public decimal UnitPrice { get; init; } = UnitPrice;

    /// <summary>Optional tax configuration to apply to this item.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa8</example>
    public Guid? TaxConfigurationId { get; init; } = TaxConfigurationId;
}
