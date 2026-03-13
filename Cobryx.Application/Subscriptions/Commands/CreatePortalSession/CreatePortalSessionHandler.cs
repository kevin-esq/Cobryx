using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Subscriptions.Commands.CreatePortalSession;

public class CreatePortalSessionHandler : IRequestHandler<CreatePortalSessionCommand, Result<string>>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeService _stripeService;

    public CreatePortalSessionHandler(
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        IStripeService stripeService)
    {
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _stripeService = stripeService;
    }

    public async Task<Result<string>> Handle(CreatePortalSessionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure<string>(DomainErrorCode.Auth.NotAuthenticated);

        var dbContext = (DbContext)_unitOfWork;
        var subscription = await dbContext.Set<TenantSubscription>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value, cancellationToken);

        if (subscription?.StripeCustomerId == null)
            return Result.Failure<string>(DomainErrorCode.Stripe.NoStripeCustomer);

        var portalUrl = await _stripeService.CreateBillingPortalSessionAsync(subscription.StripeCustomerId, request.ReturnUrl, cancellationToken);

        return Result.Success(portalUrl);
    }
}
