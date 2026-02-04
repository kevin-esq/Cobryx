using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Mfa;

public record VerifyTotpLoginCommand(string MfaToken, string Code) : IRequest<Result<AuthResult>>;

public class VerifyTotpLoginHandler : IRequestHandler<VerifyTotpLoginCommand, Result<AuthResult>>
{
    private readonly IMfaService _mfaService;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAuthService _authService;
    private readonly IHttpContextService _httpContextService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<VerifyTotpLoginHandler> _logger;
    private readonly ISecurityAuditService _auditService;
    private readonly IAuthAttemptService _attemptService;

    public VerifyTotpLoginHandler(
        IMfaService mfaService,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuthService authService,
        IHttpContextService httpContextService,
        IPasswordHasher passwordHasher,
        ILogger<VerifyTotpLoginHandler> logger,
        ISecurityAuditService auditService,
        IAuthAttemptService attemptService)
    {
        _mfaService = mfaService;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _authService = authService;
        _httpContextService = httpContextService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _auditService = auditService;
        _attemptService = attemptService;
    }

    public async Task<Result<AuthResult>> Handle(VerifyTotpLoginCommand request, CancellationToken cancellationToken)
    {
        var userId = _jwtTokenGenerator.ValidateMfaToken(request.MfaToken);
        if (userId == null) return Result.Failure<AuthResult>("Invalid or expired MFA token.");

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null) return Result.Failure<AuthResult>("User not found.");

        _logger.LogInformation("Verifying TOTP for user: {Email}", user.Email);

        var totpDevice = user.MfaDevices.FirstOrDefault(d => d.Type == Domain.Entities.MfaDeviceType.Totp && d.IsVerified);
        if (totpDevice == null) return Result.Failure<AuthResult>("No TOTP device configured.");

        bool isValid = _mfaService.VerifyCode(totpDevice.Secret, request.Code);
        if (!isValid)
        {
            var recoveryCode = user.RecoveryCodes.FirstOrDefault(rc => !rc.IsUsed && _passwordHasher.VerifyPassword(request.Code, rc.CodeHash));
            if (recoveryCode != null)
            {
                recoveryCode.Use();
                isValid = true;
            }
        }

        var ipAddress = _httpContextService.GetIpAddress();

        if (!isValid)
        {
            _auditService.LogFailure("VerifyTotp", user.Id.ToString(), ipAddress, "Invalid code");
            await _attemptService.IncrementAttemptsAsync(ipAddress);
            return Result.Failure<AuthResult>("Invalid verification code.");
        }

        await _attemptService.ResetAttemptsAsync(ipAddress);
        _auditService.LogSuccess("VerifyTotp", user.Id.ToString(), ipAddress);
        var deviceFingerprint = _httpContextService.GetDeviceFingerprint();
        var userAgent = _httpContextService.GetUserAgent();
        var authResult = _authService.GenerateAuthResponse(user, ipAddress, deviceFingerprint, userAgent, null);

        await _userRepository.UpdateAsync(user);

        return Result.Success(authResult);
    }
}
