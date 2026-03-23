using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Validation;
using Cobryx.Domain.Exceptions.System;
using Cobryx.Domain.Exceptions.Users;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Identity.Enums;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Auth.Commands.Register;

public record SignUpCommand(
    string BusinessName,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    bool MarketingConsent = false,
    string? TermsVersion = CobryxDefaults.TermsVersion,
    string? CaptchaToken = null,
    string? ReturnUrl = null) : IRequest<Result<Guid>>;

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

public class SignUpHandler(
    ITenantRepository tenantRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher,
    IHttpContextService httpContextService,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IOptions<AppOptions> appOptions,
    ILogger<SignUpHandler> logger) : IRequestHandler<SignUpCommand, Result<Guid>>
{
    private readonly ITenantRepository _tenantRepository = tenantRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly IHttpContextService _httpContextService = httpContextService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEmailService _emailService = emailService;
    private readonly AppOptions _appOptions = appOptions.Value;
    private readonly ILogger<SignUpHandler> _logger = logger;

    public async Task<Result<Guid>> Handle(SignUpCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new UserEmailAlreadyExistsException();

        var ownerRole = await _roleRepository.GetByNameAsync(Role.Constants.Owner, cancellationToken)
            ?? throw new SystemConfigurationException();

        var tenant = new Tenant(request.BusinessName);
        await _tenantRepository.AddAsync(tenant, cancellationToken);

        var consent = new Cobryx.Domain.ValueObjects.LegalConsent(
            true,
            request.TermsVersion ?? CobryxDefaults.TermsVersion,
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

        var dbContext = (DbContext)_unitOfWork;
        var starterPlan = await dbContext.Set<SubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.Tier == PlanTier.Starter && p.IsActive, cancellationToken);

        if (starterPlan != null)
        {
            var subscription = new TenantSubscription(tenant.Id, starterPlan.Id, DateTime.UtcNow);
            dbContext.Set<TenantSubscription>().Add(subscription);
        }

        var growthMetrics = new TenantGrowthMetrics(tenant.Id, DateTime.UtcNow);
        growthMetrics.RecordTrialStart(DateTime.UtcNow, starterPlan?.TrialDays ?? 14);
        dbContext.Set<TenantGrowthMetrics>().Add(growthMetrics);

        var baseUrl = request.ReturnUrl ?? _appOptions.AppUrl;
        await _emailService.SendEmailAsync(
            user.Email,
            "Verifica tu cuenta Cobryx",
            $"Hola {user.FirstName}, por favor verifica tu cuenta haciendo clic aquí: {baseUrl}/verify?token={tokenValue}",
            cancellationToken);

        return Result.Success(user.Id);
    }
}
