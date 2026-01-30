using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Concordia;
using FluentValidation;

namespace Cobryx.Application.Auth.Commands.Core;

// --- Verify Email ---
public record VerifyEmailCommand(string Token) : IRequest<Result>;

public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, Result>
{
    private readonly IUserRepository _userRepository;

    public VerifyEmailHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        // TODO: We need a method to find user by token. For now, this is inefficient but works.
        // Ideally: Add GetByToken to IUserRepository or use a direct query.
        // Assuming we have to query all or filter. 
        // OPTIMIZATION: Created a method in repo to find by security token.

        var user = await _userRepository.GetBySecurityTokenAsync(request.Token, SecurityTokenType.EmailVerification);

        if (user == null)
        {
            return Result.Failure("Invalid or expired token.");
        }

        var token = user.SecurityTokens.FirstOrDefault(t => t.Token == request.Token && t.Type == SecurityTokenType.EmailVerification);

        if (token == null || !token.IsActive)
        {
            return Result.Failure("Invalid or expired token.");
        }

        token.Use();
        user.VerifyEmail();

        await _userRepository.UpdateAsync(user);

        return Result.Success();
    }
}

// --- Resend Verification ---
public record ResendVerificationCommand(string Email) : IRequest<Result>;

public class ResendVerificationHandler : IRequestHandler<ResendVerificationCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public ResendVerificationHandler(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<Result> Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null) return Result.Success(); // Silent success to prevent enumeration

        if (user.IsEmailVerified) return Result.Success();

        // Expire old tokens
        foreach (var oldToken in user.SecurityTokens.Where(t => t.Type == SecurityTokenType.EmailVerification && t.IsActive))
        {
            oldToken.Revoke();
        }

        var tokenValue = Guid.NewGuid().ToString("N"); // Simple opaque token
        user.AddSecurityToken(tokenValue, SecurityTokenType.EmailVerification, 24 * 60); // 24 hours

        await _userRepository.UpdateAsync(user);

        // Send Email
        await _emailService.SendEmailAsync(user.Email, "Verifica tu cuenta Cobryx",
            $"Por favor verifica tu cuenta haciendo clic aquí: https://app.cobryx.com/verify?token={tokenValue}", cancellationToken);

        return Result.Success();
    }
}
