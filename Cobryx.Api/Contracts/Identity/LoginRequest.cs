namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for user authentication (Login).
/// </summary>
public record LoginRequest(
    string Email,
    string Password,
    string? CaptchaToken = null
);
