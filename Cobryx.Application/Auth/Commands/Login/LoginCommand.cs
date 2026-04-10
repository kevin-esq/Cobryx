using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Application.Common.Validation;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using FluentValidation;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Login
{
    public record LoginCommand(string Email, string Password, string? DeviceName = null, string? CaptchaToken = null) : IRequest<Result<AuthResult>>;

    public class LoginValidator : AbstractValidator<LoginCommand>
    {
        public LoginValidator()
        {
            _ = RuleFor(static x => x.Email)
                .NotEmpty().WithErrorCode(AuthValidationErrors.Email.Required)
                .EmailAddress().WithErrorCode(AuthValidationErrors.Email.Invalid);
            _ = RuleFor(static x => x.Password)
                .NotEmpty().WithErrorCode(AuthValidationErrors.Password.Required);
        }
    }

    public class LoginHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthService authService,
        ITenantProvider tenantProvider,
        ILogger<LoginHandler> logger,
        IHttpContextService httpContextService,
        ISecurityAuditService auditService,
        IAuthAttemptService attemptService,
        IClock clock,
        CobryxMetrics metrics) : IRequestHandler<LoginCommand, Result<AuthResult>>
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IAuthService _authService = authService;
        private readonly ITenantProvider _tenantProvider = tenantProvider;
        private readonly ILogger<LoginHandler> _logger = logger;
        private readonly IHttpContextService _httpContextService = httpContextService;
        private readonly ISecurityAuditService _auditService = auditService;
        private readonly IAuthAttemptService _attemptService = attemptService;
        private readonly IClock _clock = clock;
        private readonly CobryxMetrics _metrics = metrics;

        public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var startTime = _clock.UtcNow;
            var ipAddress = _httpContextService.GetIpAddress();

            try
            {
                var ipAttempts = await _attemptService.GetAttemptCountAsync(ipAddress);
                var userAttempts = await _attemptService.GetUserAttemptCountAsync(request.Email);
                var maxAttempts = Math.Max(ipAttempts, userAttempts);

                if (maxAttempts >= 10)
                {
                    var backoffSeconds = Math.Min(3600, Math.Pow(2, maxAttempts - 10) * 5);
                    _logger.LogWarning("Login lockout: {Email} from {IP}. Backoff: {Backoff}s", request.Email, ipAddress, backoffSeconds);

                    await EnsureUniformTiming(startTime, 500, cancellationToken);
                    return Result.Failure<AuthResult>(DomainErrorCode.Auth.InvalidCredentials);
                }

                var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
                var isValid = false;
                var needsRehash = false;

                if (user is { IsActive: true, IsLocked: false })
                {
                    isValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
                    if (isValid)
                    {
                        if (!user.IsEmailVerified)
                        {
                            _auditService.LogFailure("Login", user.Id.ToString(), ipAddress, "Email not verified");
                            await _attemptService.IncrementAttemptsAsync(ipAddress, request.Email);
                            await EnsureUniformTiming(startTime, 500, cancellationToken);
                            return Result.Failure<AuthResult>(DomainErrorCode.Auth.InvalidCredentials);
                        }
                        needsRehash = _passwordHasher.IsHashOutdated(user.PasswordHash);
                    }
                }
                else
                {
                    _ = _passwordHasher.VerifyPassword(request.Password, "v1.10.65536.4.YmFzZTY0c2FsdA==.YmFzZTY0aGFzaA==");
                }

                if (!isValid || user == null)
                {
                    _auditService.LogFailure("Login", user?.Id.ToString(), ipAddress, "Invalid credentials or account state");
                    await _attemptService.IncrementAttemptsAsync(ipAddress, request.Email);

                    await EnsureUniformTiming(startTime, 500, cancellationToken);
                    return Result.Failure<AuthResult>(DomainErrorCode.Auth.InvalidCredentials);
                }

                await _attemptService.ResetAttemptsAsync(ipAddress, request.Email);
                _auditService.LogSuccess("Login", user.Id.ToString(), ipAddress);

                if (needsRehash)
                {
                    _logger.LogInformation("Promoting password hash for {Email}", user.Email);
                    user.SetPasswordHash(_passwordHasher.HashPassword(request.Password));
                    await _userRepository.UpdateAsync(user, cancellationToken);
                }

                if (user.IsMfaEnabled)
                {
                    await EnsureUniformTiming(startTime, 500, cancellationToken);
                    return Result.Success(_authService.GenerateMfaPartialResponse(user));
                }

                var deviceFingerprint = _httpContextService.GetDeviceFingerprint();
                var userAgent = _httpContextService.GetUserAgent();
                var authResult = _authService.GenerateAuthResponse(user, ipAddress, deviceFingerprint, userAgent, request.DeviceName);

                _tenantProvider.SetTenantId(user.TenantId);

                await EnsureUniformTiming(startTime, 500, cancellationToken);
                _metrics.LoginSuccesses.Add(1);

                return Result.Success(authResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login processing failed unexpectedly for {Email}", request.Email);
                await EnsureUniformTiming(startTime, 500, cancellationToken);
                return Result.Failure<AuthResult>(DomainErrorCode.Auth.InvalidCredentials);
            }
        }

        private async Task EnsureUniformTiming(DateTime startTime, int targetMs, CancellationToken ct)
        {
            var elapsed = (_clock.UtcNow - startTime).TotalMilliseconds;
            var remaining = targetMs - elapsed;
            if (remaining > 0)
            {
                await Task.Delay((int)remaining, ct);
            }
            await Task.Delay(Random.Shared.Next(10, 50), ct);
        }
    }
}
