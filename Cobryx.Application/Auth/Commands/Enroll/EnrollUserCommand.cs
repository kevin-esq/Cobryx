using System.Diagnostics;
using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
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
    private readonly CobryxMetrics _metrics;
    private readonly AppOptions _appOptions;
    private readonly ILogger<EnrollUserHandler> _logger;

    public EnrollUserHandler(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        ISubscriptionEnforcementService enforcementService,
        IPasswordHasher passwordHasher,
        IHttpContextService httpContextService,
        CobryxMetrics metrics,
        IOptions<AppOptions> appOptions,
        ILogger<EnrollUserHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _enforcementService = enforcementService;
        _passwordHasher = passwordHasher;
        _httpContextService = httpContextService;
        _metrics = metrics;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(EnrollUserCommand request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var dbContext = (DbContext)_unitOfWork;

        var secret = _appOptions.InvitationTokenSecret;
        var oldSecret = _appOptions.OldInvitationTokenSecret;

        var currentHash = TokenHasher.GetHmacHash(request.Token, secret);

        var secondaryHash = !string.IsNullOrEmpty(oldSecret)
            ? TokenHasher.GetHmacHash(request.Token, oldSecret)
            : TokenHasher.GetHmacHash(request.Token, "CONSTANT_COST_PADDING_SECRET");

        var invitation = await dbContext.Set<TenantInvitation>()
            .FirstOrDefaultAsync(x => x.TokenHash == currentHash || x.TokenHash == secondaryHash, ct);

        Result<Guid> result;
        if (invitation == null)
        {
            _metrics.InvitationsRejected.Add(1, new KeyValuePair<string, object?>("reason", "invalid_token"));
            result = Result.Failure<Guid>(DomainErrorCode.Auth.InvalidToken);
        }
        else if (!invitation.IsActive())
        {
            _metrics.InvitationsReplayAttempts.Add(1, new KeyValuePair<string, object?>("status", invitation.Status.ToString()));
            _logger.LogWarning("[REPLAY] Attempt to use non-active invitation: ID {Id}, Status {Status}", invitation.Id, invitation.Status);
            result = Result.Failure<Guid>(DomainErrorCode.Auth.InvalidToken);
        }
        else if (!string.Equals(invitation.Email, request.Email.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
        {
            _metrics.InvitationsRejected.Add(1, new KeyValuePair<string, object?>("reason", "email_mismatch"));
            _logger.LogWarning("[SECURITY] Enrollment email mismatch: Expected {Expected}, Actual {Actual}", invitation.Email, request.Email);
            result = Result.Failure<Guid>(DomainErrorCode.Auth.EmailMismatch);
        }
        else
        {
            try
            {
                await _enforcementService.EnsureWithinUsersLimitAsync(invitation.TenantId, ct);

                if (await _userRepository.ExistsByEmailAsync(request.Email, ct))
                {
                    result = Result.Failure<Guid>(DomainErrorCode.User.AlreadyExists);
                }
                else
                {
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

                    _metrics.InvitationsAccepted.Add(1);
                    _logger.LogInformation("[AUDIT] Invited user enrolled: ID {UserId}, Email {Email}, Tenant {TenantId}", user.Id, request.Email, invitation.TenantId);
                    result = Result.Success(user.Id);
                }
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("SubscriptionLimitExceededException"))
            {
                result = Result.Failure<Guid>(DomainErrorCode.Subscription.LimitExceeded);
            }
        }

        sw.Stop();
        var remaining = _appOptions.MinEnrollmentResponseTimeMs - (int)sw.ElapsedMilliseconds;
        if (remaining > 0)
        {
            await Task.Delay(remaining, ct);
        }

        return result;
    }
}
