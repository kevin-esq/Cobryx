using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Auth.Commands.Core;

[PublicRequest]
public record ExternalLoginCommand(ExternalProvider Provider, string IdToken, string? CaptchaToken = null) : IRequest<Result<AuthResult>>;

public class ExternalLoginHandler(
    IUserRepository userRepository,
    IExternalAuthService externalAuthService,
    IAuthService authService,
    ITenantRepository tenantRepository,
    IHttpContextService httpContextService) : IRequestHandler<ExternalLoginCommand, Result<AuthResult>>
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IExternalAuthService _externalAuthService = externalAuthService;
    private readonly IAuthService _authService = authService;
    private readonly ITenantRepository _tenantRepository = tenantRepository;
    private readonly IHttpContextService _httpContextService = httpContextService;

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

        var user = await _userRepository.GetByEmailAsync(externalUser.Email, cancellationToken);

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

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success(authResponse);
    }
}
