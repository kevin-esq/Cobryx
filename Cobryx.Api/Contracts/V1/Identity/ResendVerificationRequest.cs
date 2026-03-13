namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for requesting a new verification email.
/// </summary>
public record ResendVerificationRequest(string Email, string? CaptchaToken = null, string? ReturnUrl = null)
{
    /// <summary>User's registered email address.</summary>
    /// <example>owner@acme.mx</example>
    public string Email { get; init; } = Email;

    /// <summary>Optional captcha verification token.</summary>
    /// <example>1x00000000000000000000AA</example>
    public string? CaptchaToken { get; init; } = CaptchaToken;

    /// <summary>Optional base URL for the verification link.</summary>
    /// <example>https://dashboard.acme.mx/verify</example>
    public string? ReturnUrl { get; init; } = ReturnUrl;
}
