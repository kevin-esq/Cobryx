using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Identity.Enums;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Tenants.Commands.InviteUser
{
    [TenantScoped]
    [RequiresLimit(PlanLimitType.Users)]
    public record InviteUserCommand(string Email, string RoleName) : IRequest<Result<Guid>>, IRequiresTenant;

    public class InviteUserValidator : AbstractValidator<InviteUserCommand>
    {
        public InviteUserValidator()
        {
            _ = RuleFor(static x => x.Email).NotEmpty().EmailAddress();
            _ = RuleFor(static x => x.RoleName).NotEmpty();
        }
    }

    public class InviteUserHandler(
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IEmailService emailService,
        IClock clock,
        CobryxMetrics metrics,
        IOptions<AppOptions> appOptions,
        ILogger<InviteUserHandler> logger) : IRequestHandler<InviteUserCommand, Result<Guid>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ITenantProvider _tenantProvider = tenantProvider;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IRoleRepository _roleRepository = roleRepository;
        private readonly IEmailService _emailService = emailService;
        private readonly IClock _clock = clock;
        private readonly CobryxMetrics _metrics = metrics;
        private readonly AppOptions _appOptions = appOptions.Value;
        private readonly ILogger<InviteUserHandler> _logger = logger;

        public async Task<Result<Guid>> Handle(InviteUserCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _tenantProvider.GetTenantId();
            if (!tenantId.HasValue)
            {
                return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);
            }

            if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            {
                return Result.Failure<Guid>(DomainErrorCode.User.AlreadyExists);
            }

            var role = await _roleRepository.GetByNameAsync(request.RoleName, cancellationToken);
            if (role == null || role.Name == Role.Constants.Owner)
            {
                return Result.Failure<Guid>(DomainErrorCode.Auth.InvalidRole);
            }

            var dbContext = (DbContext)_unitOfWork;
            var normalizedEmail = request.Email.ToLowerInvariant();
            var existingInvite = await dbContext.Set<TenantInvitation>()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.Email == normalizedEmail && x.Status == InvitationStatus.Pending, cancellationToken);

            var rawToken = TokenHasher.GenerateSecureToken();
            var tokenHash = TokenHasher.GetHmacHash(rawToken, _appOptions.InvitationTokenSecret);
            var expiresAt = _clock.UtcNow.AddHours(_appOptions.InvitationTokenTTLHours);

            if (existingInvite != null)
            {
                _ = dbContext.Set<TenantInvitation>().Remove(existingInvite);
                _ = await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("[AUDIT] Replacing pending invitation for {Email} in Tenant {TenantId}", request.Email, tenantId.Value);
            }

            var invitation = new TenantInvitation(request.Email, tenantId.Value, role.Id, tokenHash, expiresAt);
            _ = dbContext.Set<TenantInvitation>().Add(invitation);

            _ = await _unitOfWork.SaveChangesAsync(cancellationToken);

            var inviteUrl = $"{_appOptions.AppUrl}/enroll?token={rawToken}&email={Uri.EscapeDataString(request.Email)}";
            await _emailService.SendEmailAsync(
                request.Email,
                "Invitación a unirse a Cobryx",
                $"Has sido invitado a unirse a un equipo en Cobryx. Haz clic aquí para configurar tu cuenta: <a href='{inviteUrl}'>{inviteUrl}</a>",
                cancellationToken);

            _metrics.InvitationsCreated.Add(1);
            _logger.LogInformation("[AUDIT] Invitation created: ID {Id}, Email {Email}, Tenant {TenantId}, Role {Role}",
                invitation.Id, request.Email, tenantId.Value, request.RoleName);

            var db = (DbContext)_unitOfWork;
            var usersCount = await db.Set<User>().CountAsync(u => u.TenantId == tenantId.Value, cancellationToken);
            if (usersCount == 1)
            {
                var tenant = await db.Set<Tenant>().FirstAsync(t => t.Id == tenantId.Value, cancellationToken);
                tenant.TriggerOnboardingMilestone("BUILDING_TEAM");
            }

            return Result.Success(invitation.Id);
        }
    }
}
