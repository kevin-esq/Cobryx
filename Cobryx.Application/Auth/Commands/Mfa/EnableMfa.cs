using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Common;
using Concordia;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Mfa;

public record EnableMfaCommand(string Secret, string Code) : IRequest<Result<List<string>>>;

public class EnableMfaHandler : IRequestHandler<EnableMfaCommand, Result<List<string>>>
{
    private readonly IMfaService _mfaService;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<EnableMfaHandler> _logger;

    public EnableMfaHandler(
        IMfaService mfaService,
        IUserRepository userRepository,
        ICurrentUserProvider currentUserProvider,
        IPasswordHasher passwordHasher,
        ILogger<EnableMfaHandler> logger)
    {
        _mfaService = mfaService;
        _userRepository = userRepository;
        _currentUserProvider = currentUserProvider;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<List<string>>> Handle(EnableMfaCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserProvider.GetUserId();
        if (userId == null) return Result.Failure<List<string>>(DomainErrorCode.Auth.NotAuthenticated);

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null) return Result.Failure<List<string>>(DomainErrorCode.User.NotFound);

        if (user.IsMfaEnabled) return Result.Failure<List<string>>(DomainErrorCode.Auth.MfaAlreadyEnabled);

        _logger.LogInformation("Enabling MFA for user: {Email}", user.Email);

        bool isValid = _mfaService.VerifyCode(request.Secret, request.Code);
        if (!isValid) return Result.Failure<List<string>>(DomainErrorCode.Auth.InvalidMfaCode);

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

        await _userRepository.UpdateAsync(user);

        return Result.Success(rawRecoveryCodes);
    }
}
