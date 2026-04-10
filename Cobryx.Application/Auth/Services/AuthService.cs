using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Services
{
    public partial class AuthService(
        IJwtTokenGenerator jwtTokenGenerator,
        ISecurityAuditService auditService,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<AuthService> logger) : IAuthService
    {
        private static readonly TimeSpan _refreshTokenLifetime = TimeSpan.FromDays(7);
        private static readonly TimeSpan _accessTokenLifetime = TimeSpan.FromMinutes(60);

        public AuthResult GenerateAuthResponse(
            User user,
            string ipAddress,
            string? deviceFingerprint,
            string? userAgent,
            string? deviceName = null,
            Role? roleOverride = null)
        {
            try
            {
                user.CreateProfile();
                _ = user.DetectAndAlertNewDevice(ipAddress, userAgent);

                var session = user.AddSession(ipAddress, deviceFingerprint, userAgent, deviceName);

                var now = clock.UtcNow;
                var refreshExpires = now.Add(_refreshTokenLifetime);

                var accessToken = jwtTokenGenerator.GenerateAccessToken(user, session.Id, roleOverride);
                var refreshTokenValue = jwtTokenGenerator.GenerateRefreshToken();

                refreshTokenRepository.Add(
                    new RefreshToken(refreshTokenValue, refreshExpires, ipAddress, user.Id, session.Id));

                return CreateAuthResult(user, accessToken, refreshTokenValue, session.Id, now, refreshExpires,
                    roleOverride);
            }
            catch (Exception ex)
            {
                LogAuthResponseGenerationError(logger, ex, user.Id);
                throw;
            }
        }

        public async Task<AuthResult> RefreshAuthResponse(
            User user,
            RefreshToken token,
            string ipAddress,
            string? deviceFingerprint,
            CancellationToken cancellationToken = default)
        {
            if (token.IsRevoked)
            {
                auditService.LogSecurityAlert(
                    "RefreshTokenReuse",
                    user.Id.ToString(),
                    ipAddress,
                    "Revoked token used. Critical: Invalidate session family.");

                await refreshTokenRepository.RevokeAllForSessionAsync(
                    token.SessionId,
                    "Token reuse detected",
                    cancellationToken);

                user.RevokeSession(token.SessionId);
                _ = await unitOfWork.SaveChangesAsync(cancellationToken);

                throw new DomainException(DomainErrorCode.Auth.TokenCompromised);
            }

            if (token.IsExpired)
            {
                throw new DomainException(DomainErrorCode.Auth.TokenExpired);
            }

            var session = user.Sessions.FirstOrDefault(s => s.Id == token.SessionId);

            if (session is null || session.IsRevoked)
            {
                throw new DomainException(DomainErrorCode.Auth.SessionRevoked);
            }

            var now = clock.UtcNow;
            var refreshExpires = now.Add(_refreshTokenLifetime);

            var newRefreshTokenValue = jwtTokenGenerator.GenerateRefreshToken();

            token.Revoke(ipAddress, newRefreshTokenValue);

            refreshTokenRepository.Add(
                new RefreshToken(newRefreshTokenValue, refreshExpires, ipAddress, user.Id, session.Id));

            var accessToken = jwtTokenGenerator.GenerateAccessToken(user, session.Id);

            session.UpdateActivity();

            return CreateAuthResult(user, accessToken, newRefreshTokenValue, session.Id, now, refreshExpires);
        }

        public AuthResult GenerateMfaPartialResponse(User user)
        {
            var mfaToken = jwtTokenGenerator.GenerateMfaToken(user);

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

        private static AuthResult CreateAuthResult(
            User user,
            string accessToken,
            string refreshToken,
            Guid sessionId,
            DateTime now,
            DateTime refreshExpires,
            Role? roleOverride = null)
        {
            return new AuthResult(
                accessToken,
                refreshToken,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.Email,
                roleOverride?.Name ?? user.Role.Name,
                now.Add(_accessTokenLifetime),
                refreshExpires,
                sessionId,
                RequiresOnboarding: user.RequiresOnboarding
            );
        }
    }
}
