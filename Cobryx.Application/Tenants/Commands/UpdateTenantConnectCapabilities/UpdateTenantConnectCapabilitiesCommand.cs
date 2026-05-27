using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Tenants.Commands.UpdateTenantConnectCapabilities;

[TenantScoped]
public record UpdateTenantConnectCapabilitiesCommand(
    string StripeAccountId,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted) : IRequest<Result>, IRequiresTenant;

public class UpdateTenantConnectCapabilitiesHandler : IRequestHandler<UpdateTenantConnectCapabilitiesCommand, Result>
{
    private readonly ICobryxDbContext _dbContext;
    private readonly ILogger<UpdateTenantConnectCapabilitiesHandler> _logger;

    public UpdateTenantConnectCapabilitiesHandler(
        ICobryxDbContext dbContext,
        ILogger<UpdateTenantConnectCapabilitiesHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateTenantConnectCapabilitiesCommand request, CancellationToken ct)
    {
        var tenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.StripeAccountId == request.StripeAccountId, ct);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant with Stripe Account ID {StripeAccountId} not found. Skipping Connect capability update.", request.StripeAccountId);
            return Result.Success();
        }

        tenant.UpdateConnectStatus(request.ChargesEnabled, request.PayoutsEnabled, request.DetailsSubmitted);

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Updated Connect capabilities for Tenant {TenantId} ({StripeAccountId}): Charges={C}, Payouts={P}, Details={D}",
            tenant.Id, request.StripeAccountId, request.ChargesEnabled, request.PayoutsEnabled, request.DetailsSubmitted);

        return Result.Success();
    }
}
