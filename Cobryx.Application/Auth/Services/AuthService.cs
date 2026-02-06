using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Cobryx.Domain.Common;

namespace Cobryx.Application.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ISecurityAuditService _auditService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IJwtTokenGenerator jwtTokenGenerator,
        ISecurityAuditService auditService,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        ILogger<AuthService> logger)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _auditService = auditService;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public AuthResult GenerateAuthResponse(User user, string ipAddress, string? deviceFingerprint, string? userAgent, string? deviceName = null, Role? roleOverride = null)
    {
        try
        {
            user.CreateProfile();

            user.DetectAndAlertNewDevice(ipAddress, userAgent);

            var session = user.AddSession(ipAddress, deviceFingerprint, userAgent, deviceName);
            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, session.Id, roleOverride);
            var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();

            var expires = DateTime.UtcNow.AddDays(7);
            var refreshToken = new RefreshToken(refreshTokenValue, expires, ipAddress, user.Id, session.Id);
            _refreshTokenRepository.Add(refreshToken);

            return CreateAuthResult(user, accessToken, refreshTokenValue, session.Id, expires, roleOverride);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating auth response for user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<AuthResult> RefreshAuthResponse(User user, RefreshToken token, string ipAddress, string? deviceFingerprint, CancellationToken cancellationToken = default)
    {
        if (token.IsRevoked)
        {
            _auditService.LogSecurityAlert("RefreshTokenReuse", user.Id.ToString(), ipAddress, $"Revoked token used. Critical: Invalidate session family.");

            await _refreshTokenRepository.RevokeAllForSessionAsync(token.SessionId, "Token reuse detected", cancellationToken);
            user.RevokeSession(token.SessionId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new DomainException(DomainErrorCodes.Auth.TokenCompromised);
        }

        if (token.IsExpired)
        {
            throw new DomainException(DomainErrorCodes.Auth.TokenExpired);
        }

        var session = user.Sessions.FirstOrDefault(s => s.Id == token.SessionId);
        if (session == null || session.IsRevoked)
        {
            throw new DomainException(DomainErrorCodes.Auth.SessionRevoked);
        }

        var newRefreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();
        token.Revoke(ipAddress, newRefreshTokenValue);

        var expires = DateTime.UtcNow.AddDays(7);
        var newRefreshToken = new RefreshToken(newRefreshTokenValue, expires, ipAddress, user.Id, session.Id);
        _refreshTokenRepository.Add(newRefreshToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, session.Id);

        session.UpdateActivity();

        return CreateAuthResult(user, accessToken, newRefreshTokenValue, session.Id, expires);
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
            MfaToken: mfaToken,
            RequiresOnboarding: user.RequiresOnboarding
        );
    }

    private AuthResult CreateAuthResult(User user, string accessToken, string refreshToken, Guid sessionId, DateTime refreshExpires, Role? roleOverride = null)
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
            refreshExpires,
            sessionId,
            RequiresOnboarding: user.RequiresOnboarding
        );
    }
}
