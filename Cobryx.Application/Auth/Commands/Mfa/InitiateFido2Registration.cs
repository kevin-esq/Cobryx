using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Fido2NetLib;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Mfa;

public record InitiateFido2RegistrationCommand : IRequest<Result<CredentialCreateOptions>>;

public class InitiateFido2RegistrationHandler : IRequestHandler<InitiateFido2RegistrationCommand, Result<CredentialCreateOptions>>
{
    private readonly IFido2Service _fido2Service;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ILogger<InitiateFido2RegistrationHandler> _logger;

    public InitiateFido2RegistrationHandler(
        IFido2Service fido2Service,
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider,
        ILogger<InitiateFido2RegistrationHandler> logger)
    {
        _fido2Service = fido2Service;
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
        _logger = logger;
    }

    public async Task<Result<CredentialCreateOptions>> Handle(InitiateFido2RegistrationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserProvider.GetUserId();
        if (userId == null) return Result.Failure<CredentialCreateOptions>(DomainErrorCode.Auth.NotAuthenticated);

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null) return Result.Failure<CredentialCreateOptions>(DomainErrorCode.User.NotFound);

        _logger.LogInformation("Initiating FIDO2 registration for user: {Email}", user.Email);

        var options = _fido2Service.InitiateRegistration(user);

        return Result.Success(options);
    }
}
