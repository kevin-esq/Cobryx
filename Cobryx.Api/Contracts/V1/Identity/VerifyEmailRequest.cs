using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public contract for verifying an email address.
/// </summary>
public record VerifyEmailRequest(
    [Required] string Token
)
{
    /// <summary>Unique identifier for the verification token.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public string Token { get; init; } = Token;
}
