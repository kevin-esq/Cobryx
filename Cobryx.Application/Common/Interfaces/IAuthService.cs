using Cobryx.Application.Auth.Common;
using Cobryx.Domain.Entities;

namespace Cobryx.Application.Common.Interfaces;

public interface IAuthService
{
    AuthResult GenerateAuthResponse(User user, string ipAddress, string? deviceFingerprint, string? userAgent, string? deviceName = null, Role? roleOverride = null);
    Task<AuthResult> RefreshAuthResponse(User user, RefreshToken token, string ipAddress, string? deviceFingerprint, CancellationToken cancellationToken = default);
    AuthResult GenerateMfaPartialResponse(User user);
}
