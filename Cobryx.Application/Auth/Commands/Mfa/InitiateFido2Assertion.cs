using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;
using Fido2NetLib;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Mfa;

public record InitiateFido2AssertionCommand(string MfaToken) : IRequest<Result<AssertionOptions>>;

public class InitiateFido2AssertionHandler : IRequestHandler<InitiateFido2AssertionCommand, Result<AssertionOptions>>
{
    private readonly IFido2Service _fido2Service;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<InitiateFido2AssertionHandler> _logger;

    public InitiateFido2AssertionHandler(
        IFido2Service fido2Service,
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<InitiateFido2AssertionHandler> logger)
    {
        _fido2Service = fido2Service;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<Result<AssertionOptions>> Handle(InitiateFido2AssertionCommand request, CancellationToken cancellationToken)
    {
        var userId = _jwtTokenGenerator.ValidateMfaToken(request.MfaToken);
        if (userId == null) return Result.Failure<AssertionOptions>("Invalid or expired MFA token.");

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null) return Result.Failure<AssertionOptions>("User not found.");

        _logger.LogInformation("Initiating FIDO2 assertion for user: {Email}", user.Email);

        var options = _fido2Service.InitiateAssertion(user);

        return Result.Success(options);
    }
}
