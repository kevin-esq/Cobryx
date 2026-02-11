using System.Text.Json.Serialization;

namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Result of an authentication or login attempt.
/// </summary>
public record AuthResponseContract(
    string? AccessToken,
    string? FirstName,
    string? LastName,
    string? FullName,
    string? Email,
    string? Role,
    DateTime? Expires,
    Guid? SessionId = null,
    bool RequiresMfa = false,
    string? MfaToken = null,
    bool RequiresOnboarding = false
);
