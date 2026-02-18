using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;
using Fido2NetLib;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Mfa;

public record CompleteFido2RegistrationCommand(
    string DeviceName,
    AuthenticatorAttestationRawResponse Response,
    CredentialCreateOptions Options) : IRequest<Result<bool>>;

public class CompleteFido2RegistrationHandler : IRequestHandler<CompleteFido2RegistrationCommand, Result<bool>>
{
    private readonly IFido2Service _fido2Service;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ILogger<CompleteFido2RegistrationHandler> _logger;

    public CompleteFido2RegistrationHandler(
        IFido2Service fido2Service,
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider,
        ILogger<CompleteFido2RegistrationHandler> logger)
    {
        _fido2Service = fido2Service;
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(CompleteFido2RegistrationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserProvider.GetUserId();
        if (userId == null) return Result.Failure<bool>(DomainErrorCode.Auth.NotAuthenticated);

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null) return Result.Failure<bool>(DomainErrorCode.User.NotFound);

        _logger.LogInformation("Completing FIDO2 registration for user: {Email}", user.Email);

        var device = await _fido2Service.CompleteRegistrationAsync(user, request.DeviceName, request.Response, request.Options, cancellationToken);
        device.Verify();
        user.AddMfaDevice(device);

        if (!user.IsMfaEnabled)
        {
            user.EnableMfa();
        }

        await _userRepository.UpdateAsync(user);

        return Result.Success(true);
    }
}
