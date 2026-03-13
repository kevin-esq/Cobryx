using Cobryx.Application.Auth.Common;
using Cobryx.Domain.Identity;

namespace Cobryx.Application.Common.Interfaces;

public interface IAuthService
{
    public AuthResult GenerateAuthResponse(User user, string ipAddress, string? deviceFingerprint, string? userAgent, string? deviceName = null, Role? roleOverride = null);
    public Task<AuthResult> RefreshAuthResponse(User user, RefreshToken token, string ipAddress, string? deviceFingerprint, CancellationToken cancellationToken = default);
    public AuthResult GenerateMfaPartialResponse(User user);
}
