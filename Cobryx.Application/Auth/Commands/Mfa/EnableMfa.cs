using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Mfa;

public record EnableMfaCommand(string Secret, string Code) : IRequest<Result<List<string>>>;

public class EnableMfaHandler(
    IMfaService mfaService,
    IUserRepository userRepository,
    ICurrentUserProvider currentUserProvider,
    IPasswordHasher passwordHasher,
    ILogger<EnableMfaHandler> logger) : IRequestHandler<EnableMfaCommand, Result<List<string>>>
{
    private readonly IMfaService _mfaService = mfaService;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ICurrentUserProvider _currentUserProvider = currentUserProvider;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ILogger<EnableMfaHandler> _logger = logger;

    public async Task<Result<List<string>>> Handle(EnableMfaCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserProvider.GetUserId();
        if (userId == null)
            return Result.Failure<List<string>>(DomainErrorCode.Auth.NotAuthenticated);

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
            return Result.Failure<List<string>>(DomainErrorCode.User.NotFound);

        if (user.IsMfaEnabled)
            return Result.Failure<List<string>>(DomainErrorCode.Auth.MfaAlreadyEnabled);

        _logger.LogInformation("Enabling MFA for user: {Email}", user.Email);

        bool isValid = _mfaService.VerifyCode(request.Secret, request.Code);
        if (!isValid)
            return Result.Failure<List<string>>(DomainErrorCode.Auth.InvalidMfaCode);

        var device = new MfaDevice(user.Id, "Default Authenticator", MfaDeviceType.Totp, request.Secret);
        device.Verify();
        user.AddMfaDevice(device);

        var rawRecoveryCodes = _mfaService.GenerateRecoveryCodes();
        foreach (var code in rawRecoveryCodes)
        {
            var hashedCode = _passwordHasher.HashPassword(code);
            user.AddRecoveryCode(new RecoveryCode(user.Id, hashedCode));
        }

        user.EnableMfa();

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success(rawRecoveryCodes);
    }
}
