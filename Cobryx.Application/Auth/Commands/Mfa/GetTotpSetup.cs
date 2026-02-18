using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Common;
using Concordia;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Mfa;

public record TotpSetupResult(string Secret, string QrCodeUri);

public record GetTotpSetupQuery : IRequest<Result<TotpSetupResult>>;

public class GetTotpSetupHandler : IRequestHandler<GetTotpSetupQuery, Result<TotpSetupResult>>
{
    private readonly IMfaService _mfaService;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ILogger<GetTotpSetupHandler> _logger;

    public GetTotpSetupHandler(
        IMfaService mfaService,
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider,
        ILogger<GetTotpSetupHandler> logger)
    {
        _mfaService = mfaService;
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
        _logger = logger;
    }

    public async Task<Result<TotpSetupResult>> Handle(GetTotpSetupQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserProvider.GetUserId();
        if (userId == null) return Result.Failure<TotpSetupResult>(DomainErrorCode.Auth.NotAuthenticated);

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null) return Result.Failure<TotpSetupResult>(DomainErrorCode.User.NotFound);

        _logger.LogInformation("Generating TOTP setup for user: {Email}", user.Email);

        var secret = _mfaService.GenerateSecret();
        var qrCodeUri = _mfaService.GetQrCodeUri(user, secret);

        return Result.Success(new TotpSetupResult(secret, qrCodeUri));
    }
}
