namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Public contract for registering a new payment method (e.g. Bank Account, Cash).
/// </summary>
public record CreatePaymentMethodRequest(
    string Name,
    string Code,
    string? Description = null
)
{
    /// <summary>User-friendly name for the payment method.</summary>
    /// <example>BBVA Bank Transfer</example>
    public string Name { get; init; } = Name;

    /// <summary>Machine-readable code for the method.</summary>
    /// <example>BANK_TRANSFER</example>
    public string Code { get; init; } = Code;

    /// <summary>Optional description of how to use this method.</summary>
    /// <example>Direct deposit to Account 1234567890</example>
    public string? Description { get; init; } = Description;
}
