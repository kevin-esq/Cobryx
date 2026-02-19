using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Application.Tenants.Common;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Concordia;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Tenants.Commands.InviteUser;

[RequiresLimit(PlanLimitType.Users)]
public record InviteUserCommand(string Email, string RoleName) : IRequest<Result<Guid>>;

public class InviteUserValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.RoleName).NotEmpty();
    }
}

public class InviteUserHandler : IRequestHandler<InviteUserCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IEmailService _emailService;
    private readonly AppOptions _appOptions;
    private readonly ILogger<InviteUserHandler> _logger;

    public InviteUserHandler(
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IEmailService emailService,
        IOptions<AppOptions> appOptions,
        ILogger<InviteUserHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _emailService = emailService;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(InviteUserCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);

        // 1. Check if user already exists
        if (await _userRepository.ExistsByEmailAsync(request.Email, ct))
        {
            return Result.Failure<Guid>(DomainErrorCode.User.AlreadyExists);
        }

        // 2. Validate Role
        var role = await _roleRepository.GetByNameAsync(request.RoleName, ct);
        if (role == null || role.Name == Role.Constants.Owner)
        {
            return Result.Failure<Guid>(DomainErrorCode.Auth.InvalidRole);
        }

        // 3. Handle existing invitation
        var dbContext = (DbContext)_unitOfWork;
        var normalizedEmail = request.Email.ToLowerInvariant();
        var existingInvite = await dbContext.Set<TenantInvitation>()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId.Value && x.Email == normalizedEmail, ct);

        string rawToken = Guid.NewGuid().ToString("N");
        string tokenHash = TokenHasher.GetHmacHash(rawToken, _appOptions.JwtSecret);
        var expiresAt = DateTime.UtcNow.AddDays(3);

        if (existingInvite != null)
        {
            if (existingInvite.Status == InvitationStatus.Accepted)
            {
                 return Result.Failure<Guid>(DomainErrorCode.User.AlreadyExists);
            }
            
            // Remove old pending/expired/revoked invitation to replace it
            dbContext.Set<TenantInvitation>().Remove(existingInvite);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var invitation = new TenantInvitation(request.Email, tenantId.Value, role.Id, tokenHash, expiresAt);
        dbContext.Set<TenantInvitation>().Add(invitation);
        
        await _unitOfWork.SaveChangesAsync(ct);

        // 4. Send Email
        var inviteUrl = $"{_appOptions.AppUrl}/enroll?token={rawToken}&email={Uri.EscapeDataString(request.Email)}";
        await _emailService.SendEmailAsync(
            request.Email,
            "Invitación a unirse a Cobryx",
            $"Has sido invitado a unirse a un equipo en Cobryx. Haz clic aquí para configurar tu cuenta: <a href='{inviteUrl}'>{inviteUrl}</a>",
            ct);

        _logger.LogInformation("Invitation sent to {Email} for Tenant {TenantId}", request.Email, tenantId.Value);

        return Result.Success(invitation.Id);
    }
}
