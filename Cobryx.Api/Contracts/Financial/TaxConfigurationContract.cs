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
);
