using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Tenants.Commands.GetConnectOnboardingLink;

[TenantScoped]
public record GetConnectOnboardingLinkCommand(string ReturnUrl, string RefreshUrl) : IRequest<Result<string>>, IRequiresTenant;

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
        if (tenantId == null)
            return Result.Failure<string>(DomainErrorCode.Auth.NotAuthenticated);

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant == null)
            return Result.Failure<string>(DomainErrorCode.Common.GeneralError);

        if (string.IsNullOrEmpty(tenant.StripeAccountId))
        {
            var owner = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Role.Name == Role.Constants.Owner, ct);

            if (owner == null)
            {
                owner = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Role.Name == Role.Constants.Admin, ct);
            }

            var onboardingEmail = owner?.Email.Value ?? "onboarding@cobryx.com";

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
