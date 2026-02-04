using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Auth.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;
using FluentValidation;
using Cobryx.Domain.Enums;
using Cobryx.Application.Common.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Register;

public record SignUpCommand(
    string BusinessName,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    bool MarketingConsent = false,
    string? TermsVersion = "v1.0",
    string? CaptchaToken = null) : IRequest<Result>;

public class SignUpValidator : AbstractValidator<SignUpCommand>
{
    public SignUpValidator()
    {
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12);
    }
}

public class SignUpHandler : IRequestHandler<SignUpCommand, Result>
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

    public async Task<Result> Handle(SignUpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
                return Result.Failure("Email already registered.");

            var ownerRole = await _roleRepository.GetByNameAsync("Owner", cancellationToken);
            if (ownerRole == null) return Result.Failure("System roles not initialized.");

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

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignUp failed for {Email}", request.Email);
            throw;
        }
    }
}
