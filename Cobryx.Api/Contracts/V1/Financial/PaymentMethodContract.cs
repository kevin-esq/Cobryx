using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Professional summary of a tenant's payment method.
/// </summary>
public record PaymentMethodContract(
    Guid Id,
    string Name,
    string Code,
    string? Description
)
{
    /// <summary>Unique identifier for the payment method.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [Required]
    public Guid Id { get; init; } = Id;

    /// <summary>User-friendly name of the method.</summary>
    /// <example>BBVA Bank Transfer</example>
    [Required]
    public string Name { get; init; } = Name;

    /// <summary>Internal machine-readable code.</summary>
    /// <example>BANK_TRANSFER</example>
    [Required]
    public string Code { get; init; } = Code;

    /// <summary>Optional account or usage instructions.</summary>
    /// <example>Account: 1234567890, CLABE: 012345678901234567</example>
    public string? Description { get; init; } = Description;
}
