using System;

namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Professional summary of a financial product or service.
/// </summary>
public record ProductSummaryContract(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency
)
{
    /// <summary>Unique identifier for the product.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; } = Id;

    /// <summary>Display name of the product.</summary>
    /// <example>Acme Pro Plan</example>
    public string Name { get; init; } = Name;

    /// <summary>A brief summary of the product's features.</summary>
    /// <example>Full access to all premium modules and priority support.</example>
    public string Description { get; init; } = Description;

    /// <summary>The current listing price.</summary>
    /// <example>1299.00</example>
    public decimal Price { get; init; } = Price;

    /// <summary>The pricing currency (ISO 4217).</summary>
    /// <example>MXN</example>
    public string Currency { get; init; } = Currency;
}
