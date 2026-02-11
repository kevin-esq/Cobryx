using System.Text.Json.Serialization;

namespace Cobryx.Application.Auth.Common;

public record AuthResult(
    string? Token,
    [property: JsonIgnore] string? RefreshToken,
    string? FirstName,
    string? LastName,
    string? FullName,
    string? Email,
    string? Role,
    DateTime? Expires,
    DateTime? RefreshExpires = null,
    Guid? SessionId = null,
    bool RequiresMfa = false,
    string? MfaToken = null,
    bool RequiresOnboarding = false);
