using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire recurring job that syncs Stripe Connect capabilities for all connected tenants.
/// </summary>
public class TenantConnectSyncJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeService _stripeService;
    private readonly ILogger<TenantConnectSyncJob> _logger;

    public TenantConnectSyncJob(
        IUnitOfWork unitOfWork,
        IStripeService stripeService,
        ILogger<TenantConnectSyncJob> logger)
    {
        _unitOfWork = unitOfWork;
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var db = (DbContext)_unitOfWork;

        // 1. Find all tenants with a Connect Account
        var connectedTenants = await db.Set<Tenant>()
            .Where(t => t.StripeAccountId != null && t.StripeAccountId != "")
            .ToListAsync(ct);

        _logger.LogInformation("Starting Connect capability sync for {Count} tenants.", connectedTenants.Count);

        foreach (var tenant in connectedTenants)
        {
            try
            {
                var status = await _stripeService.GetConnectAccountStatusAsync(tenant.StripeAccountId!, ct);

                tenant.UpdateConnectStatus(
                    status.ChargesEnabled,
                    status.PayoutsEnabled,
                    status.DetailsSubmitted);

                _logger.LogDebug("Synced Connect status for Tenant {TenantId}: Charges={C}, Payouts={P}, Details={D}",
                    tenant.Id, status.ChargesEnabled, status.PayoutsEnabled, status.DetailsSubmitted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync Connect status for Tenant {TenantId} ({AccountId})",
                    tenant.Id, tenant.StripeAccountId);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Connect capability sync completed.");
    }
}
