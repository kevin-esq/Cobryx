namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Public contract for creating a tax configuration (e.g. VAT, Sales Tax).
/// </summary>
public record CreateTaxRequest(
    string Name,
    decimal Rate,
    bool IsInclusive = false,
    bool IsDefault = false
)
{
    /// <summary>User-friendly name of the tax rule.</summary>
    /// <example>IVA (VAT)</example>
    public string Name { get; init; } = Name;

    /// <summary>The tax rate as a decimal (0.16 = 16%).</summary>
    /// <example>0.16</example>
    public decimal Rate { get; init; } = Rate;

    /// <summary>True if the tax is already included in product prices.</summary>
    /// <example>true</example>
    public bool IsInclusive { get; init; } = IsInclusive;

    /// <summary>True if this tax should be automatically applied to new items.</summary>
    /// <example>true</example>
    public bool IsDefault { get; init; } = IsDefault;
}
