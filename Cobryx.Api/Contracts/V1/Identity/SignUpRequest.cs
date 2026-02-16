using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for user registration and tenant creation.
/// </summary>
public record SignUpRequest(
    [Required] string FirstName,
    [Required] string LastName,
    [Required] string BusinessName,
    [Required] string Email,
    [Required] string Password,
    string? ReturnUrl = null
)
{
    /// <summary>User's first name.</summary>
    /// <example>Jane</example>
    public string FirstName { get; init; } = FirstName;

    /// <summary>User's last name.</summary>
    /// <example>Smith</example>
    public string LastName { get; init; } = LastName;

    /// <summary>Human-readable name for the new tenant.</summary>
    /// <example>Acme Corp</example>
    public string BusinessName { get; init; } = BusinessName;

    /// <summary>User's email address.</summary>
    /// <example>owner@acme.mx</example>
    public string Email { get; init; } = Email;

    /// <summary>User's password (min 12 chars).</summary>
    /// <example>P@ssw0rd123!</example>
    public string Password { get; init; } = Password;

    /// <summary>Optional base URL for the verification link.</summary>
    /// <example>https://dashboard.acme.mx/auth/verify</example>
    public string? ReturnUrl { get; init; } = ReturnUrl;
}
