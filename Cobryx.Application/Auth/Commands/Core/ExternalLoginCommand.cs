using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Auth.Services;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Domain.Common;
using Cobryx.Application.Auth.Common;

namespace Cobryx.Application.Auth.Commands.Core;

public record ExternalLoginCommand(ExternalProvider Provider, string IdToken, string? CaptchaToken = null) : IRequest<Result<AuthResult>>;

public class ExternalLoginHandler : IRequestHandler<ExternalLoginCommand, Result<AuthResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IExternalAuthService _externalAuthService;
    private readonly IAuthService _authService;
    private readonly ITenantRepository _tenantRepository;
    private readonly IHttpContextService _httpContextService;

    public ExternalLoginHandler(
        IUserRepository userRepository,
        IExternalAuthService externalAuthService,
        IAuthService authService,
        ITenantRepository tenantRepository,
        IHttpContextService httpContextService)
    {
        _userRepository = userRepository;
        _externalAuthService = externalAuthService;
        _authService = authService;
        _tenantRepository = tenantRepository;
        _httpContextService = httpContextService;
    }

    public async Task<Result<AuthResult>> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        var externalUserResult = await _externalAuthService.VerifyTokenAsync(request.Provider, request.IdToken, cancellationToken);
        if (!externalUserResult.IsSuccess)
        {
            return Result.Failure<AuthResult>(externalUserResult.Error ?? DomainErrorCode.Auth.ExternalLoginFailed);
        }

        var externalUser = externalUserResult.Value;
        if (externalUser == null)
        {
            return Result.Failure<AuthResult>(DomainErrorCode.Auth.ExternalLoginFailed);
        }

        var user = await _userRepository.GetByEmailAsync(externalUser.Email);

        if (user == null)
        {
            return Result.Failure<AuthResult>(DomainErrorCode.User.NotRegistered);
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthResult>(DomainErrorCode.Auth.AccountInactive);
        }

        if (!user.IsEmailVerified)
        {
            return Result.Failure<AuthResult>(DomainErrorCode.Auth.EmailNotVerified);
        }

        var ipAddress = _httpContextService.GetIpAddress() ?? CobryxDefaults.FallbackIpAddress;
        var deviceFingerprint = _httpContextService.GetDeviceFingerprint();
        var userAgent = _httpContextService.GetUserAgent();

        var authResponse = _authService.GenerateAuthResponse(user, ipAddress, deviceFingerprint, userAgent, null);

        await _userRepository.UpdateAsync(user);

        return Result.Success(authResponse);
    }
}
