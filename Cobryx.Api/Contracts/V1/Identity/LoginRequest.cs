using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for user authentication (Login).
/// </summary>
public record LoginRequest(
    [Required] string Email,
    [Required] string Password,
    string? CaptchaToken = null
)
{
    /// <summary>User's registered email address.</summary>
    /// <example>user@example.com</example>
    public string Email { get; init; } = Email;

    /// <summary>User's password.</summary>
    /// <example>SecurePassword123!</example>
    public string Password { get; init; } = Password;

    /// <summary>Optional captcha verification token.</summary>
    /// <example>1x00000000000000000000AA</example>
    public string? CaptchaToken { get; init; } = CaptchaToken;
}
