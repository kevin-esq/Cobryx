namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Public contract for creating a new product or inventory item.
/// </summary>
public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    Guid? CategoryId = null
);
