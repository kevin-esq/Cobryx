using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Tenants.Common;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Tenants.Queries.GetPendingInvitations;

public record GetPendingInvitationsQuery : IRequest<Result<List<InvitationDto>>>;

public class GetPendingInvitationsHandler : IRequestHandler<GetPendingInvitationsQuery, Result<List<InvitationDto>>>
{
    private readonly ICobryxDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetPendingInvitationsHandler(ICobryxDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<List<InvitationDto>>> Handle(GetPendingInvitationsQuery request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<List<InvitationDto>>(DomainErrorCode.Tenant.ContextMissing);

        var invitations = await _context.TenantInvitations
            .Where(x => x.TenantId == tenantId.Value && x.Status == InvitationStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .Join(_context.Roles,
                inv => inv.RoleId,
                role => role.Id,
                (inv, role) => new InvitationDto(
                    inv.Id,
                    inv.Email,
                    role.Name,
                    inv.Status,
                    inv.ExpiresAt,
                    inv.CreatedAt
                ))
            .ToListAsync(ct);

        return Result.Success(invitations);
    }
}
