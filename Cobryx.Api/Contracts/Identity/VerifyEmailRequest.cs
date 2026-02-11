namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for verifying an email address.
/// </summary>
public record VerifyEmailRequest(string Token);
