namespace Cobryx.Application.Auth.Common;

public record AuthResult(
    string? Token,
    string? RefreshToken,
    string? FirstName,
    string? LastName,
    string? FullName,
    string? Email,
    string? Role,
    DateTime? Expires,
    Guid? SessionId = null,
    bool RequiresMfa = false,
    string? MfaToken = null);
