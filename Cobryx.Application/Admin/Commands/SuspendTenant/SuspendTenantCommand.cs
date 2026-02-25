using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Enums;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Admin.Commands.SuspendTenant;

public record SuspendTenantCommand(Guid TenantId, string Reason) : IRequest<Result>;

public class SuspendTenantHandler : IRequestHandler<SuspendTenantCommand, Result>
{
    private readonly ICobryxDbContext _dbContext;
    private readonly ICurrentUserProvider _currentUserProvider;

    public SuspendTenantHandler(ICobryxDbContext dbContext, ICurrentUserProvider currentUserProvider)
    {
        _dbContext = dbContext;
        _currentUserProvider = currentUserProvider;
    }

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
            return Result.Success(); // Idempotent
        }

        var oldStatus = tenant.Status;

        // 1. Domain Action
        tenant.Suspend();

        // 2. Audit Trail
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
