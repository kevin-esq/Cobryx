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
using Microsoft.Extensions.Options;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Auth.Commands.Register
{
    [PublicRequest]
public sealed record SignUpCommand(
        string BusinessName,
        string FirstName,
        string LastName,
        string Email,
        string Password,
        bool MarketingConsent = false,
        string? TermsVersion = CobryxDefaults.TermsVersion,
        string? CaptchaToken = null,
        string? ReturnUrl = null) : IRequest<Result<Guid>>;

    public sealed class SignUpValidator : AbstractValidator<SignUpCommand>
    {
        public SignUpValidator()
        {
            _ = RuleFor(static x => x.BusinessName)
                .NotEmpty().WithErrorCode(AuthValidationErrors.BusinessName.Required)
                .MaximumLength(100).WithErrorCode(AuthValidationErrors.BusinessName.TooLong);

            _ = RuleFor(static x => x.FirstName)
                .NotEmpty().WithErrorCode(AuthValidationErrors.FirstName.Required)
                .MaximumLength(100).WithErrorCode(AuthValidationErrors.FirstName.TooLong);

            _ = RuleFor(static x => x.LastName)
                .NotEmpty().WithErrorCode(AuthValidationErrors.LastName.Required)
                .MaximumLength(100).WithErrorCode(AuthValidationErrors.LastName.TooLong);

            _ = RuleFor(static x => x.Email)
                .NotEmpty().WithErrorCode(AuthValidationErrors.Email.Required)
                .EmailAddress().WithErrorCode(AuthValidationErrors.Email.Invalid);

            _ = RuleFor(static x => x.Password)
                .NotEmpty().WithErrorCode(AuthValidationErrors.Password.Required)
                .MinimumLength(12).WithErrorCode(AuthValidationErrors.Password.TooShort);
        }
    }

    public sealed class SignUpHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IHttpContextService httpContextService,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IClock clock,
        IOptions<AppOptions> appOptions) : IRequestHandler<SignUpCommand, Result<Guid>>
    {
        private readonly AppOptions _appOptions = appOptions.Value;

        public async Task<Result<Guid>> Handle(SignUpCommand request, CancellationToken cancellationToken)
        {
            if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            {
                throw new UserEmailAlreadyExistsException();
            }

            var ownerRole = await roleRepository.GetByNameAsync(Role.Constants.Owner, cancellationToken)
                            ?? throw new SystemConfigurationException();

            var tenant = new Tenant(request.BusinessName);
            await tenantRepository.AddAsync(tenant, cancellationToken);

            var consent = new Domain.ValueObjects.LegalConsent(
                true,
                request.TermsVersion ?? CobryxDefaults.TermsVersion,
                httpContextService.GetIpAddress(),
                httpContextService.GetUserAgent());

            var user = User.Register(
                tenant.Id,
                request.FirstName,
                request.LastName,
                request.Email,
                ownerRole.Id,
                consent,
                request.MarketingConsent);

            user.SetPasswordHash(passwordHasher.HashPassword(request.Password));
            user.CreateProfile();

            var tokenValue = Guid.NewGuid().ToString("N");
            var tokenHash = TokenHasher.ComputeHash(tokenValue);

            _ = user.AddSecurityToken(tokenHash, SecurityTokenType.EmailVerification, 1440);
            user.UpdateVerificationResend();

            await userRepository.AddAsync(user, cancellationToken);

            var dbContext = (DbContext)unitOfWork;

            var starterPlan = await dbContext.Set<SubscriptionPlan>()
                .Where(static p => p.Tier == PlanTier.Starter && p.IsActive)
                .Select(p => new { p.Id, p.TrialDays })
                .FirstOrDefaultAsync(cancellationToken);

            if (starterPlan is not null)
            {
                _ = dbContext.Set<TenantSubscription>()
                    .Add(new TenantSubscription(tenant.Id, starterPlan.Id, clock.UtcNow));
            }

            var now = clock.UtcNow;

            var growthMetrics = new TenantGrowthMetrics(tenant.Id, now);
            growthMetrics.RecordTrialStart(now, starterPlan?.TrialDays ?? 14);

            _ = dbContext.Set<TenantGrowthMetrics>().Add(growthMetrics);

            var baseUrl = request.ReturnUrl ?? _appOptions.AppUrl;

            await emailService.SendEmailAsync(
                user.Email,
                "Verifica tu cuenta Cobryx",
                $"Hola {user.FirstName}, por favor verifica tu cuenta haciendo clic aquí: {baseUrl}/verify?token={tokenValue}",
                cancellationToken);

            return Result.Success(user.Id);
        }
    }
}
