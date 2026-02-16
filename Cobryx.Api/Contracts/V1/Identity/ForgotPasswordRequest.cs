namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for requesting a password reset email.
/// </summary>
public record ForgotPasswordRequest(string Email, string? ReturnUrl = null)
{
    /// <summary>User's email address.</summary>
    /// <example>user@example.com</example>
    public string Email { get; init; } = Email;

    /// <summary>Optional base URL for the password reset link.</summary>
    /// <example>https://app.cobryx.mx/auth/reset-password</example>
    public string? ReturnUrl { get; init; } = ReturnUrl;
}
