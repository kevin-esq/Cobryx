using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;

namespace Cobryx.Application.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ISecurityAuditService _auditService;

    public AuthService(IJwtTokenGenerator jwtTokenGenerator, ISecurityAuditService auditService)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _auditService = auditService;
    }

    public AuthResult GenerateAuthResponse(User user, string ipAddress, string? deviceFingerprint, Role? roleOverride = null)
    {
        user.CreateProfile();

        var session = user.AddSession(ipAddress, deviceFingerprint);
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, session.Id, roleOverride);
        var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();
        var refreshToken = user.AddRefreshToken(refreshTokenValue, DateTime.UtcNow.AddDays(7), ipAddress, session.Id);

        return CreateAuthResult(user, accessToken, refreshTokenValue, session.Id, roleOverride);
    }

    public AuthResult RefreshAuthResponse(User user, string oldRefreshToken, string ipAddress, string? deviceFingerprint)
    {
        user.CreateProfile();

        var token = user.RefreshTokens.FirstOrDefault(x => x.Token == oldRefreshToken);

        if (token == null)
        {
            throw new Exception("Invalid refresh token.");
        }

        if (token.IsRevoked)
        {
            _auditService.LogSecurityAlert("RefreshTokenReuse", user.Id.ToString(), ipAddress, "A revoked refresh token was reused. Potential theft attempt.");
            user.InvalidateTokenChain(oldRefreshToken, ipAddress);
            throw new Exception("Compromised refresh token used. Session invalidated.");
        }

        var session = user.Sessions.FirstOrDefault(s => s.Id == token.SessionId);
        if (session == null || session.IsRevoked)
        {
            throw new Exception("Session is revoked or missing.");
        }


        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, session.Id);
        var newRefreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();

        token.Revoke(ipAddress, newRefreshTokenValue);
        user.AddRefreshToken(newRefreshTokenValue, DateTime.UtcNow.AddDays(7), ipAddress, session.Id);

        session.UpdateActivity();

        return CreateAuthResult(user, accessToken, newRefreshTokenValue, session.Id);
    }
    public AuthResult GenerateMfaPartialResponse(User user)
    {
        var mfaToken = _jwtTokenGenerator.GenerateMfaToken(user);

        return new AuthResult(
            Token: null,
            RefreshToken: null,
            FirstName: user.FirstName,
            LastName: user.LastName,
            FullName: user.FullName,
            Email: user.Email,
            Role: null,
            Expires: null,
            RequiresMfa: true,
            MfaToken: mfaToken
        );
    }

    private AuthResult CreateAuthResult(User user, string accessToken, string refreshToken, Guid sessionId, Role? roleOverride = null)
    {
        return new AuthResult(
            accessToken,
            refreshToken,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Email,
            roleOverride?.Name ?? user.Role?.Name ?? "User",
            DateTime.UtcNow.AddMinutes(60),
            sessionId
        );
    }
}
