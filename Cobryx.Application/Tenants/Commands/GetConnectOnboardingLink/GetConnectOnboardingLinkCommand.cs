using Concordia;
using Microsoft.EntityFrameworkCore;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Tenants.Commands.GetConnectOnboardingLink;

public record GetConnectOnboardingLinkCommand(string ReturnUrl, string RefreshUrl) : IRequest<Result<string>>;

public class GetConnectOnboardingLinkHandler : IRequestHandler<GetConnectOnboardingLinkCommand, Result<string>>
{
    private readonly ICobryxDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<GetConnectOnboardingLinkHandler> _logger;

    public GetConnectOnboardingLinkHandler(
        ICobryxDbContext context,
        IStripeService stripeService,
        ITenantProvider tenantProvider,
        ILogger<GetConnectOnboardingLinkHandler> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(GetConnectOnboardingLinkCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Result.Failure<string>(DomainErrorCode.Auth.NotAuthenticated);

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant == null) return Result.Failure<string>(DomainErrorCode.Common.GeneralError);

        // 1. Ensure Stripe Account exists
        if (string.IsNullOrEmpty(tenant.StripeAccountId))
        {
            try
            {
                // We'll use the tenant's business name and email (TODO: Fetch primary admin email if needed)
                var stripeAccountId = await _stripeService.CreateConnectAccountAsync(
                    "onboarding@cobryx.com", // Placeholder or fetch correct email
                    tenant.BusinessName,
                    ct);

                tenant.SetStripeAccountId(stripeAccountId);
                await _context.SaveChangesAsync(ct);

                _logger.LogInformation("Created Stripe Connect Account {AccountId} for Tenant {TenantId}", stripeAccountId, tenant.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Stripe Connect account for Tenant {TenantId}", tenant.Id);
                return Result.Failure<string>(DomainErrorCode.Common.GeneralError);
            }
        }

        // 2. Generate Onboarding Link
        try
        {
            var url = await _stripeService.CreateConnectOnboardingLinkAsync(
                tenant.StripeAccountId!,
                request.ReturnUrl,
                request.RefreshUrl,
                ct);

            return Result.Success(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Stripe Onboarding link for Tenant {TenantId}", tenant.Id);
            return Result.Failure<string>(DomainErrorCode.Common.GeneralError);
        }
    }
}
