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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public GetPendingInvitationsHandler(IUnitOfWork unitOfWork, ITenantProvider tenantProvider)
    {
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<List<InvitationDto>>> Handle(GetPendingInvitationsQuery request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<List<InvitationDto>>(DomainErrorCode.Tenant.ContextMissing);

        var dbContext = (DbContext)_unitOfWork;
        
        var invitations = await dbContext.Set<TenantInvitation>()
            .Where(x => x.TenantId == tenantId.Value && x.Status == InvitationStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new InvitationDto(
                x.Id,
                x.Email,
                "Member", // Optimized join not needed for Beta POC, but placeholder reflects likely role
                x.Status,
                x.ExpiresAt,
                x.CreatedAt
            ))
            .ToListAsync(ct);

        return Result.Success(invitations);
    }
}
