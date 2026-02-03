using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Concordia;
using FluentValidation;
using Cobryx.Application.Common.Configuration;
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
        var user = await _userRepository.GetBySecurityTokenAsync(request.Token, SecurityTokenType.EmailVerification, cancellationToken);

        if (user == null)
        {
            return Result.Failure("Invalid or expired token.");
        }

        var token = user.SecurityTokens.FirstOrDefault(t => t.Token == request.Token && t.Type == SecurityTokenType.EmailVerification);

        if (token is not { IsActive: true })
        {
            return Result.Failure("Invalid or expired token.");
        }

        token.Use();
        user.VerifyEmail();

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}


public record ResendVerificationCommand(string Email) : IRequest<Result>;

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
        if (user == null || user.IsEmailVerified)
        {
            return Result.Success();
        }

        foreach (var oldToken in user.SecurityTokens.Where(t => t.Type == SecurityTokenType.EmailVerification && t.IsActive))
        {
            oldToken.Revoke();
        }

        var tokenValue = Guid.NewGuid().ToString("N");
        user.AddSecurityToken(tokenValue, SecurityTokenType.EmailVerification, 24 * 60);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailService.SendEmailAsync(
            user.Email,
            "Verifica tu cuenta Cobryx",
            $"Por favor verifica tu cuenta haciendo clic aquí: {_appOptions.AppUrl}/verify?token={tokenValue}",
            cancellationToken);

        return Result.Success();
    }
}
