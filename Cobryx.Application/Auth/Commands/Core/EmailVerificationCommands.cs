using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Validation;
using Cobryx.Domain.Identity.Enums;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using FluentValidation;

using Microsoft.Extensions.Options;

namespace Cobryx.Application.Auth.Commands.Core;


public record VerifyEmailCommand(string Token) : IRequest<Result>;

public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyEmailHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result.Failure(DomainErrorCode.Auth.InvalidToken);
        }

        var tokenHash = TokenHasher.ComputeHash(request.Token);
        var user = await _userRepository.GetBySecurityTokenHashAsync(tokenHash, SecurityTokenType.EmailVerification, cancellationToken);

        if (user == null)
        {
            return Result.Failure(DomainErrorCode.Auth.InvalidToken);
        }

        var token = user.SecurityTokens.FirstOrDefault(t => t.TokenHash == tokenHash && t.Type == SecurityTokenType.EmailVerification);

        if (token is not { IsActive: true })
        {
            return Result.Failure(DomainErrorCode.Auth.InvalidToken);
        }

        token.Use();
        user.VerifyEmail();

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}


public record ResendVerificationCommand(string Email, string? CaptchaToken = null, string? ReturnUrl = null) : IRequest<Result>;

public class ResendVerificationValidator : AbstractValidator<ResendVerificationCommand>
{
    public ResendVerificationValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(AuthValidationErrors.Email.Required)
            .EmailAddress().WithErrorCode(AuthValidationErrors.Email.Invalid);
    }
}

public class ResendVerificationHandler : IRequestHandler<ResendVerificationCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly AppOptions _appOptions;
    private readonly IUnitOfWork _unitOfWork;

    public ResendVerificationHandler(IUserRepository userRepository, IEmailService emailService, IOptions<AppOptions> appOptions, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _appOptions = appOptions.Value;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null || user.IsEmailVerified || !user.IsActive)
        {
            return Result.Success();
        }

        var now = DateTime.UtcNow;

        if (user.LastVerificationSentAt.HasValue && user.LastVerificationSentAt.Value.Date < now.Date)
        {
            user.ResetVerificationResendCount();
        }

        if (user.LastVerificationSentAt.HasValue && (now - user.LastVerificationSentAt.Value).TotalMinutes < 2)
        {
            throw new TooManyRequestsException();
        }

        if (user.VerificationResendCount >= 5)
        {
            throw new TooManyRequestsException();
        }

        var activeTokens = user.SecurityTokens
            .Where(t => t.Type == SecurityTokenType.EmailVerification && t.IsActive)
            .ToList();

        foreach (var oldToken in activeTokens)
        {
            oldToken.Revoke();
        }

        var tokenValue = Guid.NewGuid().ToString("N");
        var tokenHash = TokenHasher.ComputeHash(tokenValue);
        user.AddSecurityToken(tokenHash, SecurityTokenType.EmailVerification, 24 * 60);

        user.UpdateVerificationResend();

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var baseUrl = request.ReturnUrl ?? _appOptions.AppUrl;
        await _emailService.SendEmailAsync(
            user.Email,
            "Verifica tu cuenta Cobryx",
            $"Hola {user.FirstName}, por favor verifica tu cuenta haciendo clic aquí: {baseUrl}/verify?token={tokenValue}",
            cancellationToken);

        return Result.Success();
    }
}
