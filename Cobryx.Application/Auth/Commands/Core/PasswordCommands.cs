using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Application.Common.Validation;
using Cobryx.Domain.Identity.Enums;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using FluentValidation;

using Microsoft.Extensions.Options;

namespace Cobryx.Application.Auth.Commands.Core;


public record ForgotPasswordCommand(string Email, string? ReturnUrl = null) : IRequest<Result>;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(AuthValidationErrors.Email.Required)
            .EmailAddress().WithErrorCode(AuthValidationErrors.Email.Invalid);
    }
}

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly AppOptions _appOptions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CobryxMetrics _metrics;

    public ForgotPasswordHandler(IUserRepository userRepository, IEmailService emailService, IOptions<AppOptions> appOptions, IUnitOfWork unitOfWork, CobryxMetrics metrics)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _appOptions = appOptions.Value;
        _unitOfWork = unitOfWork;
        _metrics = metrics;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user != null)
        {
            foreach (var oldToken in user.SecurityTokens.Where(t => t.Type == SecurityTokenType.PasswordReset && t.IsActive))
            {
                oldToken.Revoke();
            }

            var tokenValue = Guid.NewGuid().ToString("N");
            var tokenHash = TokenHasher.ComputeHash(tokenValue);
            user.AddSecurityToken(tokenHash, SecurityTokenType.PasswordReset, 60);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var baseUrl = request.ReturnUrl ?? _appOptions.AppUrl;
            await _emailService.SendEmailAsync(
                user.Email,
                "Restablecer contraseña Cobryx",
                $"Para restablecer tu contraseña, haz clic aquí: {baseUrl}/reset-password?token={tokenValue}",
                cancellationToken);

            _metrics.PasswordResets.Add(1);
        }
        else
        {
            await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);
        }

        await EnsureUniformTiming(startTime, 500, cancellationToken);
        return Result.Success();
    }

    private static async Task EnsureUniformTiming(DateTime startTime, int targetMs, CancellationToken ct)
    {
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        var remaining = targetMs - elapsed;
        if (remaining > 0)
        {
            await Task.Delay((int)remaining, ct);
        }
        await Task.Delay(Random.Shared.Next(10, 50), ct);
    }
}


public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result>;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithErrorCode(AuthValidationErrors.Token.Required);
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithErrorCode(AuthValidationErrors.Password.Required)
            .MinimumLength(12).WithErrorCode(AuthValidationErrors.Password.TooShort);
    }
}

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.ComputeHash(request.Token);
        var user = await _userRepository.GetBySecurityTokenHashAsync(tokenHash, SecurityTokenType.PasswordReset, cancellationToken);
        if (user == null)
        {
            return Result.Failure(DomainErrorCode.Auth.InvalidToken);
        }

        var token = user.SecurityTokens.FirstOrDefault(t => t.TokenHash == tokenHash && t.Type == SecurityTokenType.PasswordReset);

        if (token is not { IsActive: true })
        {
            return Result.Failure(DomainErrorCode.Auth.InvalidToken);
        }

        user.SetPasswordHash(_passwordHasher.HashPassword(request.NewPassword));
        token.Use();

        user.Sessions.Where(s => !s.IsRevoked).ToList().ForEach(s => s.Revoke());

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}


public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithErrorCode(AuthValidationErrors.Password.Required);
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithErrorCode(AuthValidationErrors.Password.Required)
            .MinimumLength(12).WithErrorCode(AuthValidationErrors.Password.TooShort)
            .Matches("[A-Z]").WithErrorCode(AuthValidationErrors.Password.NoUppercase)
            .Matches("[a-z]").WithErrorCode(AuthValidationErrors.Password.NoLowercase)
            .Matches("[0-9]").WithErrorCode(AuthValidationErrors.Password.NoNumber)
            .Matches("[^a-zA-Z0-9]").WithErrorCode(AuthValidationErrors.Password.NoSpecial);
    }
}

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserProvider currentUserProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _currentUserProvider = currentUserProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserProvider.GetUserId();
        if (!userId.HasValue) return Result.Failure(DomainErrorCode.Auth.NotAuthenticated);

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null) return Result.Failure(DomainErrorCode.User.NotFound);

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(DomainErrorCode.Auth.InvalidCredentials);
        }

        user.SetPasswordHash(_passwordHasher.HashPassword(request.NewPassword));

        user.Sessions.Where(s => !s.IsRevoked).ToList().ForEach(s => s.Revoke());

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
