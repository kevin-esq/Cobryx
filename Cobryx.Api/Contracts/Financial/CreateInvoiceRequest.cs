namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Public contract for creating a new invoice.
/// </summary>
public record CreateInvoiceRequest(
    Guid CustomerId,
    DateTime DueDate,
    List<InvoiceItemRequest> Items,
    string? Notes = null
);

/// <summary>
/// Line item for a new invoice.
/// </summary>
public record InvoiceItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    Guid? TaxConfigurationId = null
);
