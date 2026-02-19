using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Concordia;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Auth.Commands.Enroll;

public record EnrollUserCommand(
    string Token,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    bool MarketingConsent = false) : IRequest<Result<Guid>>;

public class EnrollUserValidator : AbstractValidator<EnrollUserCommand>
{
    public EnrollUserValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12);
    }
}

public class EnrollUserHandler : IRequestHandler<EnrollUserCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionEnforcementService _enforcementService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IHttpContextService _httpContextService;
    private readonly AppOptions _appOptions;
    private readonly ILogger<EnrollUserHandler> _logger;

    public EnrollUserHandler(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        ISubscriptionEnforcementService enforcementService,
        IPasswordHasher passwordHasher,
        IHttpContextService httpContextService,
        IOptions<AppOptions> appOptions,
        ILogger<EnrollUserHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _enforcementService = enforcementService;
        _passwordHasher = passwordHasher;
        _httpContextService = httpContextService;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(EnrollUserCommand request, CancellationToken ct)
    {
        var dbContext = (DbContext)_unitOfWork;
        var tokenHash = TokenHasher.GetHmacHash(request.Token, _appOptions.JwtSecret);
        
        var invitation = await dbContext.Set<TenantInvitation>()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

        if (invitation == null || !invitation.IsActive())
        {
            return Result.Failure<Guid>(DomainErrorCode.Auth.InvalidToken);
        }

        if (!string.Equals(invitation.Email, request.Email.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<Guid>(DomainErrorCode.Auth.EmailMismatch);
        }

        try
        {
            await _enforcementService.EnsureWithinUsersLimitAsync(invitation.TenantId, ct);
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("SubscriptionLimitExceededException"))
        {
            return Result.Failure<Guid>(DomainErrorCode.Subscription.LimitExceeded);
        }

        if (await _userRepository.ExistsByEmailAsync(request.Email, ct))
        {
            return Result.Failure<Guid>(DomainErrorCode.User.AlreadyExists);
        }

        var consent = new Cobryx.Domain.ValueObjects.LegalConsent(
            true,
            CobryxDefaults.TermsVersion,
            _httpContextService.GetIpAddress(),
            _httpContextService.GetUserAgent());

        var user = User.Register(
            invitation.TenantId,
            request.FirstName,
            request.LastName,
            request.Email,
            invitation.RoleId,
            consent,
            request.MarketingConsent);

        user.SetPasswordHash(_passwordHasher.HashPassword(request.Password));
        user.CreateProfile();
        
        user.VerifyEmail(); 

        await _userRepository.AddAsync(user, ct);
        
        invitation.Accept();
        
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Invited user {Email} enrolled successfully in Tenant {TenantId}", request.Email, invitation.TenantId);

        return Result.Success(user.Id);
    }
}
