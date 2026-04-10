using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Users.Commands.UpdateUserRole
{
    public record UpdateUserRoleCommand(Guid UserId, Guid RoleId) : IRequest<Result>;

    public partial class UpdateUserRoleHandler(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ITenantProvider tenantProvider,
        ICurrentUserProvider currentUserProvider,
        ILogger<UpdateUserRoleHandler> logger) : IRequestHandler<UpdateUserRoleCommand, Result>
    {
        public async Task<Result> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = tenantProvider.GetTenantId()
                            ?? throw new DomainException(DomainErrorCode.Tenant.ContextMissing);
            Guid currentUserId = currentUserProvider.GetUserId()
                                 ?? throw new DomainException(DomainErrorCode.Auth.NotAuthenticated);

            User? targetUser = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (targetUser == null || targetUser.TenantId != tenantId)
            {
                return Result.Failure(DomainErrorCode.User.NotFound);
            }

            Role? newRole = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (newRole == null)
            {
                return Result.Failure(DomainErrorCode.Auth.RoleNotFound);
            }

            Role? targetRole = await roleRepository.GetByIdAsync(targetUser.RoleId, cancellationToken);
            if (newRole.Name == Role.Constants.Owner || targetRole?.Name == Role.Constants.Owner)
            {
                return Result.Failure(DomainErrorCode.Auth.Forbidden);
            }

            if (targetRole?.Name == Role.Constants.Admin && newRole.Name != Role.Constants.Admin)
            {
                var adminCount = await userRepository.CountAdminsInTenantAsync(tenantId, cancellationToken);
                if (adminCount <= 1)
                {
                    return Result.Failure(DomainErrorCode.Auth.Forbidden);
                }
            }

            var oldRoleName = targetRole?.Name ?? CobryxDefaults.UnknownValue;
            targetUser.UpdateRole(newRole.Id);
            targetUser.IncrementPermissionVersion();

            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            LogRoleChanged(logger, targetUser.Id, oldRoleName, newRole.Name, currentUserId, tenantId);

            return Result.Success();
        }

        [LoggerMessage(Level = LogLevel.Information,
            Message = "[AUDIT] Role changed: TargetUser {TargetId}, OldRole {OldRole}, NewRole {NewRole}, ChangedBy {AdminId}, Tenant {TenantId}")]
        private static partial void LogRoleChanged(ILogger logger, Guid targetId, string oldRole, string newRole, Guid adminId, Guid tenantId);
    }
}
