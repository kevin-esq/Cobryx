using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;
using Fido2NetLib;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Mfa;

public record CompleteFido2AssertionCommand(
    string MfaToken,
    AuthenticatorAssertionRawResponse Response,
    AssertionOptions Options) : IRequest<Result<AuthResult>>;

public class CompleteFido2AssertionHandler : IRequestHandler<CompleteFido2AssertionCommand, Result<AuthResult>>
{
    private readonly IFido2Service _fido2Service;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAuthService _authService;
    private readonly IHttpContextService _httpContextService;
    private readonly ILogger<CompleteFido2AssertionHandler> _logger;

    public CompleteFido2AssertionHandler(
        IFido2Service fido2Service,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuthService authService,
        IHttpContextService httpContextService,
        ILogger<CompleteFido2AssertionHandler> logger)
    {
        _fido2Service = fido2Service;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _authService = authService;
        _httpContextService = httpContextService;
        _logger = logger;
    }

    public async Task<Result<AuthResult>> Handle(CompleteFido2AssertionCommand request, CancellationToken cancellationToken)
    {
        var userId = _jwtTokenGenerator.ValidateMfaToken(request.MfaToken);
        if (userId == null) return Result.Failure<AuthResult>("Invalid or expired MFA token.");

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null) return Result.Failure<AuthResult>("User not found.");

        _logger.LogInformation("Completing FIDO2 assertion for user: {Email}", user.Email);

        bool isValid = await _fido2Service.CompleteAssertionAsync(user, request.Response, request.Options, cancellationToken);
        if (!isValid) return Result.Failure<AuthResult>("Invalid passkey assertion.");

        var ipAddress = _httpContextService.GetIpAddress();
        var deviceFingerprint = _httpContextService.GetDeviceFingerprint();
        var authResult = _authService.GenerateAuthResponse(user, ipAddress, deviceFingerprint);

        await _userRepository.UpdateAsync(user);

        return Result.Success(authResult);
    }
}
