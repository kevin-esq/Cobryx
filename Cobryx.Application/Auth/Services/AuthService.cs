using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;

namespace Cobryx.Application.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IJwtTokenGenerator jwtTokenGenerator)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public AuthResult GenerateAuthResponse(User user, string ipAddress, string? deviceFingerprint, Role? roleOverride = null)
    {
        user.CreateProfile();

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, roleOverride);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        user.AddRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7), ipAddress);
        user.AddSession(ipAddress, deviceFingerprint);

        return CreateAuthResult(user, accessToken, refreshToken, roleOverride);
    }

    public AuthResult RefreshAuthResponse(User user, string oldRefreshToken, string ipAddress, string? deviceFingerprint)
    {
        user.CreateProfile();

        var activeToken = user.RefreshTokens.FirstOrDefault(x => x.Token == oldRefreshToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

        if (activeToken != null)
        {
            activeToken.Revoke(ipAddress, refreshToken);
        }

        user.AddRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7), ipAddress);
        user.AddSession(ipAddress, deviceFingerprint);

        return CreateAuthResult(user, accessToken, refreshToken);
    }

    private AuthResult CreateAuthResult(User user, string accessToken, string refreshToken, Role? roleOverride = null)
    {
        return new AuthResult(
            accessToken,
            refreshToken,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Email,
            roleOverride?.Name ?? user.Role?.Name ?? "User",
            DateTime.UtcNow.AddMinutes(60)
        );
    }
}
