using Concordia;
using Microsoft.EntityFrameworkCore;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Exceptions;
using Cobryx.Domain.Entities;
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
            // Fetch Primary Admin / Owner Email
            var owner = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Role.Name == Role.Constants.Owner, ct);

            if (owner == null)
            {
                // Fallback to any Admin if no explicit Owner found
                owner = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Role.Name == Role.Constants.Admin, ct);
            }

            var onboardingEmail = owner?.Email.Value ?? "onboarding@cobryx.com"; // Final fallback to system if misconfigured

            try
            {
                var stripeAccountId = await _stripeService.CreateConnectAccountAsync(
                    onboardingEmail,
                    tenant.BusinessName,
                    ct);

                tenant.SetStripeAccountId(stripeAccountId);
                await _context.SaveChangesAsync(ct);

                _logger.LogInformation("Created Stripe Connect Account {AccountId} for Tenant {TenantId} (Email: {Email})",
                    stripeAccountId, tenant.Id, onboardingEmail);
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
