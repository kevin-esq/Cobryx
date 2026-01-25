namespace Cobryx.Application.Auth.Common;

public record AuthResult(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string FullName,
    IEnumerable<string> Permissions);
