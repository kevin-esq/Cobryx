using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Auth.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;
using FluentValidation;
using Cobryx.Domain.Enums;

namespace Cobryx.Application.Auth.Commands.Register;

public record RegisterCommand(
    string BusinessName,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? TaxId = null,
    string? Industry = null,
    string? BusinessAddress = null,
    bool MarketingConsent = false,
    string? TermsVersion = "v1.0",
    string? CaptchaToken = null) : IRequest<Result>;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.BusinessName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.TaxId)
            .MaximumLength(13)
            .Matches(@"^[A-Z&Ñ]{3,4}[0-9]{2}(0[1-9]|1[0-2])(0[1-9]|[12][0-9]|3[01])[A-Z0-9]{2}[0-9A]$")
            .When(x => !string.IsNullOrEmpty(x.TaxId))
            .WithMessage("Invalid Tax ID (RFC) format.");

        RuleFor(x => x.Industry).MaximumLength(100);
        RuleFor(x => x.BusinessAddress).MaximumLength(500);
    }
}

public class RegisterHandler : IRequestHandler<RegisterCommand, Result>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IHttpContextService _httpContextService;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IHttpContextService httpContextService,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _httpContextService = httpContextService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return Result.Failure("Email already registered.");
        }

        var ownerRole = await _roleRepository.GetByNameAsync("Owner", cancellationToken);
        if (ownerRole == null) return Result.Failure("System roles not initialized.");

        var tenant = Tenant.CreateForRegistration(request.BusinessName, request.TaxId, request.Industry, request.BusinessAddress);
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
        user.AddSecurityToken(tokenValue, SecurityTokenType.EmailVerification, 24 * 60);

        await _userRepository.AddAsync(user, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
