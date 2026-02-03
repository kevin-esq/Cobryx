using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Concordia;
using FluentValidation;
using Cobryx.Application.Common.Configuration;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Auth.Commands.Core;


public record ForgotPasswordCommand(string Email) : IRequest<Result>;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly AppOptions _appOptions;
    private readonly IUnitOfWork _unitOfWork;

    public ForgotPasswordHandler(IUserRepository userRepository, IEmailService emailService, IOptions<AppOptions> appOptions, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _appOptions = appOptions.Value;
        _unitOfWork = unitOfWork;
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
            user.AddSecurityToken(tokenValue, SecurityTokenType.PasswordReset, 60);

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _emailService.SendEmailAsync(
                user.Email,
                "Restablecer contraseña Cobryx",
                $"Para restablecer tu contraseña, haz clic aquí: {_appOptions.AppUrl}/reset-password?token={tokenValue}",
                cancellationToken);
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
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12);
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
        var user = await _userRepository.GetBySecurityTokenAsync(request.Token, SecurityTokenType.PasswordReset, cancellationToken);
        if (user == null)
        {
            return Result.Failure("Invalid or expired token.");
        }

        var token = user.SecurityTokens.FirstOrDefault(t => t.Token == request.Token && t.Type == SecurityTokenType.PasswordReset);

        if (token is not { IsActive: true })
        {
            return Result.Failure("Invalid or expired token.");
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
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(12)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
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
        if (!userId.HasValue) return Result.Failure("User not authenticated.");

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null) return Result.Failure("User not found.");

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure("Invalid current password.");
        }

        user.SetPasswordHash(_passwordHasher.HashPassword(request.NewPassword));

        user.Sessions.Where(s => !s.IsRevoked).ToList().ForEach(s => s.Revoke());

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
