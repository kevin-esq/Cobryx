using System;

namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Professional summary of a taxation rule.
/// </summary>
public record TaxConfigurationContract(
    Guid Id,
    string Name,
    decimal Rate,
    bool IsInclusive,
    bool IsDefault
)
{
    /// <summary>Unique identifier for the tax configuration.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; } = Id;

    /// <summary>Display name of the tax rule.</summary>
    /// <example>IVA 16%</example>
    public string Name { get; init; } = Name;

    /// <summary>The tax rate as a decimal (0.16 = 16%).</summary>
    /// <example>0.16</example>
    public decimal Rate { get; init; } = Rate;

    /// <summary>True if the tax is already included in product prices.</summary>
    /// <example>true</example>
    public bool IsInclusive { get; init; } = IsInclusive;

    /// <summary>True if this tax is applied by default to all new items.</summary>
    /// <example>true</example>
    public bool IsDefault { get; init; } = IsDefault;
}
