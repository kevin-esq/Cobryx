namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for requesting a new verification email.
/// </summary>
public record ResendVerificationRequest(string Email, string? CaptchaToken = null);
