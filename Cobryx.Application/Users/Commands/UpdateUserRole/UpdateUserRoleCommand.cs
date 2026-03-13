using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Users.Commands.UpdateUserRole;

public record UpdateUserRoleCommand(Guid UserId, Guid RoleId) : IRequest<Result>;

public class UpdateUserRoleHandler : IRequestHandler<UpdateUserRoleCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ILogger<UpdateUserRoleHandler> _logger;

    public UpdateUserRoleHandler(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITenantProvider tenantProvider,
        ICurrentUserProvider currentUserProvider,
        ILogger<UpdateUserRoleHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _tenantProvider = tenantProvider;
        _currentUserProvider = currentUserProvider;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateUserRoleCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? throw new DomainException(DomainErrorCode.Tenant.ContextMissing);
        var currentUserId = _currentUserProvider.GetUserId() ?? throw new DomainException(DomainErrorCode.Auth.NotAuthenticated);

        // 1. Fetch target user and role
        var targetUser = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (targetUser == null || targetUser.TenantId != tenantId)
        {
            return Result.Failure(DomainErrorCode.User.NotFound);
        }

        var newRole = await _roleRepository.GetByIdAsync(request.RoleId, ct);
        if (newRole == null)
        {
            return Result.Failure(DomainErrorCode.Auth.RoleNotFound);
        }

        // 2. Safety: Owner Immutability
        // Cannot assign Owner role, and cannot change an Owner's role
        var targetRole = await _roleRepository.GetByIdAsync(targetUser.RoleId, ct);
        if (newRole.Name == Role.Constants.Owner || targetRole?.Name == Role.Constants.Owner)
        {
            return Result.Failure(DomainErrorCode.Auth.Forbidden);
        }

        // 3. Safety: Last Admin Protection (Total count >= 1)
        if (targetRole?.Name == Role.Constants.Admin && newRole.Name != Role.Constants.Admin)
        {
            var adminCount = await _userRepository.CountAdminsInTenantAsync(tenantId, ct);
            if (adminCount <= 1)
            {
                return Result.Failure(DomainErrorCode.Auth.Forbidden);
            }
        }

        // 4. Execution
        var oldRoleName = targetRole?.Name ?? "Unknown";
        targetUser.UpdateRole(newRole.Id);
        targetUser.IncrementPermissionVersion();

        await _unitOfWork.SaveChangesAsync(ct);

        // 5. Audit Logging
        _logger.LogInformation(
            "[AUDIT] Role changed: TargetUser {TargetId}, OldRole {OldRole}, NewRole {NewRole}, ChangedBy {AdminId}, Tenant {TenantId}",
            targetUser.Id, oldRoleName, newRole.Name, currentUserId, tenantId);

        return Result.Success();
    }
}
