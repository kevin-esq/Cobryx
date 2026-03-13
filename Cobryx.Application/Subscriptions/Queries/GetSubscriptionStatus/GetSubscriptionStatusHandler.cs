using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Subscriptions.Queries.GetSubscriptionStatus;

public class GetSubscriptionStatusHandler : IRequestHandler<GetSubscriptionStatusQuery, Result<SubscriptionStatusDto>>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    public GetSubscriptionStatusHandler(ITenantProvider tenantProvider, IUnitOfWork unitOfWork)
    {
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SubscriptionStatusDto>> Handle(GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure<SubscriptionStatusDto>(DomainErrorCode.Auth.NotAuthenticated);

        var dbContext = (DbContext)_unitOfWork;
        var subscription = await dbContext.Set<TenantSubscription>()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value, cancellationToken);

        if (subscription == null)
            return Result.Failure<SubscriptionStatusDto>(DomainErrorCode.Subscription.NotFound);

        var dto = new SubscriptionStatusDto(
            subscription.Status.ToString(),
            subscription.Plan?.Name ?? CobryxDefaults.UnknownValue,
            subscription.Plan?.Tier.ToString() ?? CobryxDefaults.DefaultPlanTier,
            subscription.Plan?.MaxInvoices ?? 0,
            subscription.Plan?.MaxUsers ?? 0,
            subscription.TrialEndsAtUtc,
            subscription.GracePeriodEndsAtUtc,
            subscription.StripeCustomerId != null);

        return Result.Success(dto);
    }
}
