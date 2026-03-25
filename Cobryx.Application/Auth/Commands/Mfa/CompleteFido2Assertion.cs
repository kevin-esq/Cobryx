using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Fido2NetLib;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Mfa
{
    public record CompleteFido2AssertionCommand(
        string MfaToken,
        AuthenticatorAssertionRawResponse Response,
        AssertionOptions Options) : IRequest<Result<AuthResult>>;

    public class CompleteFido2AssertionHandler(
        IFido2Service fido2Service,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuthService authService,
        IHttpContextService httpContextService,
        ILogger<CompleteFido2AssertionHandler> logger,
        ISecurityAuditService auditService,
        IAuthAttemptService attemptService) : IRequestHandler<CompleteFido2AssertionCommand, Result<AuthResult>>
    {
        private readonly IFido2Service _fido2Service = fido2Service;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator = jwtTokenGenerator;
        private readonly IAuthService _authService = authService;
        private readonly IHttpContextService _httpContextService = httpContextService;
        private readonly ILogger<CompleteFido2AssertionHandler> _logger = logger;
        private readonly ISecurityAuditService _auditService = auditService;
        private readonly IAuthAttemptService _attemptService = attemptService;

        public async Task<Result<AuthResult>> Handle(CompleteFido2AssertionCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _jwtTokenGenerator.ValidateMfaToken(request.MfaToken);
            if (userId == null)
            {
                return Result.Failure<AuthResult>(DomainErrorCode.Auth.InvalidToken);
            }

            var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
            if (user == null)
            {
                return Result.Failure<AuthResult>(DomainErrorCode.User.NotFound);
            }

            _logger.LogInformation("Completing FIDO2 assertion for user: {Email}", user.Email);

            var ipAddress = _httpContextService.GetIpAddress();
            var isValid =
                await _fido2Service.CompleteAssertionAsync(user, request.Response, request.Options, cancellationToken);

            if (!isValid)
            {
                _auditService.LogFailure("VerifyFido2", user.Id.ToString(), ipAddress, "Invalid passkey assertion");
                await _attemptService.IncrementAttemptsAsync(ipAddress);
                return Result.Failure<AuthResult>(DomainErrorCode.Auth.InvalidCredentials);
            }

            await _attemptService.ResetAttemptsAsync(ipAddress);
            _auditService.LogSuccess("VerifyFido2", user.Id.ToString(), ipAddress);
            var deviceFingerprint = _httpContextService.GetDeviceFingerprint();
            var userAgent = _httpContextService.GetUserAgent();
            var authResult = _authService.GenerateAuthResponse(user, ipAddress, deviceFingerprint, userAgent);

            await _userRepository.UpdateAsync(user, cancellationToken);

            return Result.Success(authResult);
        }
    }
}
