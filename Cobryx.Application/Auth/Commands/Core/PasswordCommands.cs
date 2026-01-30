using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Concordia;
using FluentValidation;

namespace Cobryx.Application.Auth.Commands.Core;

// --- Forgot Password ---
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

    public ForgotPasswordHandler(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user != null)
        {
            // Invalidate old reset tokens
            foreach (var oldToken in user.SecurityTokens.Where(t => t.Type == SecurityTokenType.PasswordReset && t.IsActive))
            {
                oldToken.Revoke();
            }

            // Generate new token
            var tokenValue = Guid.NewGuid().ToString("N");
            user.AddSecurityToken(tokenValue, SecurityTokenType.PasswordReset, 60); // 1 hour expiry

            await _userRepository.UpdateAsync(user);

            // Send Email
            await _emailService.SendEmailAsync(user.Email, "Restablecer contraseña Cobryx",
                $"Para restablecer tu contraseña, haz clic aquí: https://app.cobryx.com/reset-password?token={tokenValue}", cancellationToken);
        }
        else
        {
            // Dummy work to simulate DB update and email dispatch timing
            await Task.Delay(new Random().Next(50, 150));
        }

        // 1. Random Jitter to prevent timing attacks
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        var targetDelay = 300 + new Random().Next(100, 300); // Target ~400-600ms
        var remaining = targetDelay - (int)elapsed;
        if (remaining > 0) await Task.Delay(remaining);

        // 2. Uniform Response: Always success
        return Result.Success();
    }
}

// --- Reset Password ---
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

    public ResetPasswordHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetBySecurityTokenAsync(request.Token, SecurityTokenType.PasswordReset);
        if (user == null)
        {
            return Result.Failure("Invalid or expired token.");
        }

        var token = user.SecurityTokens.FirstOrDefault(t => t.Token == request.Token && t.Type == SecurityTokenType.PasswordReset);

        if (token == null || !token.IsActive)
        {
            return Result.Failure("Invalid or expired token.");
        }

        // Reset Password
        user.SetPasswordHash(_passwordHasher.HashPassword(request.NewPassword));

        // Use token
        token.Use();

        // Revoke all other sessions for security
        // Ideally we would identify the current session, but since we are resetting via email token,
        // we might just want to revoke ALL sessions to force re-login everywhere.
        user.Sessions.Where(s => !s.IsRevoked).ToList().ForEach(s => s.Revoke());

        await _userRepository.UpdateAsync(user);

        return Result.Success();
    }
}

// --- Change Password ---
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

    public ChangePasswordHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserProvider currentUserProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserProvider.GetUserId();
        if (!userId.HasValue) return Result.Failure("User not authenticated.");

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null) return Result.Failure("User not found.");

        // Verify Current Password (Re-authentication)
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure("Invalid current password.");
        }

        // Set New Password
        user.SetPasswordHash(_passwordHasher.HashPassword(request.NewPassword));

        // Revoke all other sessions for security
        user.Sessions.Where(s => !s.IsRevoked).ToList().ForEach(s => s.Revoke());

        await _userRepository.UpdateAsync(user);

        return Result.Success();
    }
}
