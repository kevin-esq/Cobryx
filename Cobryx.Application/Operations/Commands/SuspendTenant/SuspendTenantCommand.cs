using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Identity.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Operations.Commands.SuspendTenant;

[PlatformScoped]
public record SuspendTenantCommand(Guid TenantId, string Reason) : IRequest<Result>;

public class SuspendTenantHandler(ICobryxDbContext dbContext, ICurrentUserProvider currentUserProvider)
    : IRequestHandler<SuspendTenantCommand, Result>
{
    private readonly ICobryxDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUserProvider = currentUserProvider;

    public async Task<Result> Handle(SuspendTenantCommand request, CancellationToken ct)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, ct);

        if (tenant == null)
        {
            return Result.Failure(DomainErrorCode.Tenant.NotFound);
        }

        if (tenant.Status == TenantStatus.Suspended)
        {
            return Result.Success();
        }

        var oldStatus = tenant.Status;

        tenant.Suspend();

        var adminUserId = _currentUserProvider.GetUserId() ?? Guid.Empty;
        var audit = new AdminActionAudit(
            adminUserId: adminUserId,
            actionName: "Tenant.Suspend",
            targetType: "Tenant",
            targetId: tenant.Id,
            reason: request.Reason,
            metadataJson: $"{{\"oldStatus\":\"{oldStatus}\", \"newStatus\":\"{tenant.Status}\"}}"
        );

        _dbContext.AdminActionAudits.Add(audit);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
