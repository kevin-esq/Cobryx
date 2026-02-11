using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Auth.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;
using FluentValidation;
using Cobryx.Application.Common.Validation;
using Cobryx.Domain.Enums;
using Cobryx.Application.Common.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Cobryx.Domain.Exceptions.Users;
using Cobryx.Domain.Exceptions.System;

namespace Cobryx.Application.Auth.Commands.Register;

public record SignUpCommand(
    string BusinessName,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    bool MarketingConsent = false,
    string? TermsVersion = "v1.0",
    string? CaptchaToken = null) : IRequest<Result<Guid>>;

public class SignUpValidator : AbstractValidator<SignUpCommand>
{
    public SignUpValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithErrorCode(AuthValidationErrors.BusinessName.Required)
            .MaximumLength(100).WithErrorCode(AuthValidationErrors.BusinessName.TooLong);
        RuleFor(x => x.FirstName)
            .NotEmpty().WithErrorCode(AuthValidationErrors.FirstName.Required)
            .MaximumLength(100).WithErrorCode(AuthValidationErrors.FirstName.TooLong);
        RuleFor(x => x.LastName)
            .NotEmpty().WithErrorCode(AuthValidationErrors.LastName.Required)
            .MaximumLength(100).WithErrorCode(AuthValidationErrors.LastName.TooLong);
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(AuthValidationErrors.Email.Required)
            .EmailAddress().WithErrorCode(AuthValidationErrors.Email.Invalid);
        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(AuthValidationErrors.Password.Required)
            .MinimumLength(12).WithErrorCode(AuthValidationErrors.Password.TooShort);
    }
}

public class SignUpHandler : IRequestHandler<SignUpCommand, Result<Guid>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IHttpContextService _httpContextService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly AppOptions _appOptions;
    private readonly ILogger<SignUpHandler> _logger;

    public SignUpHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IHttpContextService httpContextService,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IOptions<AppOptions> appOptions,
        ILogger<SignUpHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _httpContextService = httpContextService;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(SignUpCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new UserEmailAlreadyExistsException();

        var ownerRole = await _roleRepository.GetByNameAsync("Owner", cancellationToken);
        if (ownerRole == null) throw new SystemConfigurationException();

        var tenant = new Tenant(request.BusinessName);
        await _tenantRepository.AddAsync(tenant, cancellationToken);

        var consent = new Cobryx.Domain.ValueObjects.LegalConsent(
            true,
            request.TermsVersion ?? "v1.0",
            _httpContextService.GetIpAddress(),
            _httpContextService.GetUserAgent());

        var user = User.Register(
            tenant.Id,
            request.FirstName,
            request.LastName,
            request.Email,
            ownerRole.Id,
            consent,
            request.MarketingConsent);

        user.SetPasswordHash(_passwordHasher.HashPassword(request.Password));
        user.CreateProfile();

        var tokenValue = Guid.NewGuid().ToString("N");
        var tokenHash = TokenHasher.ComputeHash(tokenValue);
        user.AddSecurityToken(tokenHash, SecurityTokenType.EmailVerification, 24 * 60);

        user.UpdateVerificationResend();

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailService.SendEmailAsync(
            user.Email,
            "Verifica tu cuenta Cobryx",
            $"Hola {user.FirstName}, por favor verifica tu cuenta haciendo clic aquí: {_appOptions.AppUrl}/verify?token={tokenValue}",
            cancellationToken);

        return Result.Success(user.Id);
    }
}
