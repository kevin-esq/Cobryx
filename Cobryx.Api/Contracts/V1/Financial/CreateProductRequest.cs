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
)
{
    /// <summary>Display name of the product or service.</summary>
    /// <example>Standard Monthly Subscription</example>
    public string Name { get; init; } = Name;

    /// <summary>Brief internal or public description.</summary>
    /// <example>Standard tier Internet service with 50Mbps speed.</example>
    public string? Description { get; init; } = Description;

    /// <summary>The base price of the item.</summary>
    /// <example>599.99</example>
    public decimal Price { get; init; } = Price;

    /// <summary>The currency code (ISO 4217).</summary>
    /// <example>MXN</example>
    public string Currency { get; init; } = Currency;

    /// <summary>Optional category identifier for grouping.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa9</example>
    public Guid? CategoryId { get; init; } = CategoryId;
}
