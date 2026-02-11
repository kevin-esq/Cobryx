namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Public contract for creating a tax configuration (e.g. VAT, Sales Tax).
/// </summary>
public record CreateTaxRequest(
    string Name,
    decimal Rate,
    bool IsInclusive = false,
    bool IsDefault = false
);
