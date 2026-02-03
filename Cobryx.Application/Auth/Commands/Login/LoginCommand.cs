using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Auth.Common;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password, string? CaptchaToken = null) : IRequest<Result<AuthResult>>;

public class LoginHandler : IRequestHandler<LoginCommand, Result<AuthResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthService _authService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IHttpContextService _httpContextService;
    private readonly ISecurityAuditService _auditService;
    private readonly IAuthAttemptService _attemptService;

    public LoginHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthService authService,
        ITenantProvider tenantProvider,
        ILogger<LoginHandler> logger,
        IHttpContextService httpContextService,
        ISecurityAuditService auditService,
        IAuthAttemptService attemptService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authService = authService;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _httpContextService = httpContextService;
        _auditService = auditService;
        _attemptService = attemptService;
    }

    public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var ipAddress = _httpContextService.GetIpAddress();

        try
        {
            var ipAttempts = await _attemptService.GetAttemptCountAsync(ipAddress);
            var userAttempts = await _attemptService.GetUserAttemptCountAsync(request.Email);
            var maxAttempts = Math.Max(ipAttempts, userAttempts);

            if (maxAttempts >= 5)
            {
                var backoffSeconds = Math.Min(3600, Math.Pow(2, maxAttempts - 5) * 5);
                _logger.LogWarning("Login lockout: {Email} from {IP}. Backoff: {Backoff}s", request.Email, ipAddress, backoffSeconds);

                await EnsureUniformTiming(startTime, 500, cancellationToken);
                return Result.Failure<AuthResult>("Invalid credentials.");
            }

            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            bool isValid = false;
            bool needsRehash = false;

            if (user is { IsActive: true, IsEmailVerified: true })
            {
                isValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
                if (isValid)
                {
                    needsRehash = _passwordHasher.IsHashOutdated(user.PasswordHash);
                }
            }
            else
            {
                _passwordHasher.VerifyPassword(request.Password, "v1.10.65536.4.YmFzZTY0c2FsdA==.YmFzZTY0aGFzaA==");
            }

            if (!isValid || user == null)
            {
                _auditService.LogFailure("Login", user?.Id.ToString(), ipAddress, "Invalid credentials or account state");
                await _attemptService.IncrementAttemptsAsync(ipAddress, request.Email);

                await EnsureUniformTiming(startTime, 500, cancellationToken);
                return Result.Failure<AuthResult>("Invalid credentials.");
            }

            await _attemptService.ResetAttemptsAsync(ipAddress, request.Email);
            _auditService.LogSuccess("Login", user.Id.ToString(), ipAddress);

            if (needsRehash)
            {
                _logger.LogInformation("Promoting password hash for {Email}", user.Email);
                user.SetPasswordHash(_passwordHasher.HashPassword(request.Password));
                await _userRepository.UpdateAsync(user);
            }

            if (user.IsMfaEnabled)
            {
                await EnsureUniformTiming(startTime, 500, cancellationToken);
                return Result.Success(_authService.GenerateMfaPartialResponse(user));
            }

            var deviceFingerprint = _httpContextService.GetDeviceFingerprint();
            var userAgent = _httpContextService.GetUserAgent();
            var authResult = _authService.GenerateAuthResponse(user, ipAddress, deviceFingerprint, userAgent);

            _tenantProvider.SetTenantId(user.TenantId);

            await EnsureUniformTiming(startTime, 500, cancellationToken);
            return Result.Success(authResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login processing failed for {Email}", request.Email);
            await EnsureUniformTiming(startTime, 500, cancellationToken);
            return Result.Failure<AuthResult>("Invalid credentials.");
        }
    }

    private static async Task EnsureUniformTiming(DateTime startTime, int targetMs, CancellationToken ct)
    {
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        var remaining = targetMs - elapsed;
        if (remaining > 0)
        {
            await Task.Delay((int)remaining, ct);
        }
        await Task.Delay(Random.Shared.Next(10, 50), ct);
    }
}