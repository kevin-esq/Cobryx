using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Tenants.Commands.RevokeInvitation;

public record RevokeInvitationCommand(Guid InvitationId) : IRequest<Result>;

public class RevokeInvitationHandler : IRequestHandler<RevokeInvitationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public RevokeInvitationHandler(IUnitOfWork unitOfWork, ITenantProvider tenantProvider)
    {
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result> Handle(RevokeInvitationCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        var dbContext = (DbContext)_unitOfWork;
        var invitation = await dbContext.Set<TenantInvitation>()
            .FirstOrDefaultAsync(x => x.Id == request.InvitationId && x.TenantId == tenantId.Value, ct);

        if (invitation == null)
        {
            return Result.Failure(DomainErrorCode.Auth.InvitationNotFound);
        }

        try
        {
            invitation.Revoke();
            await _unitOfWork.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(DomainErrorCode.Auth.InvalidState);
        }
    }
}
