using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Subscriptions.Commands.CreateCheckoutSession;

public record CreateCheckoutSessionCommand(Guid PlanId, string? SuccessUrl = null, string? CancelUrl = null) : IRequest<Result<string>>;

public class CreateCheckoutSessionHandler : IRequestHandler<CreateCheckoutSessionCommand, Result<string>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeService _stripeService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<CreateCheckoutSessionHandler> _logger;

    public CreateCheckoutSessionHandler(
        IUnitOfWork unitOfWork,
        IStripeService stripeService,
        ITenantProvider tenantProvider,
        ILogger<CreateCheckoutSessionHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _stripeService = stripeService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null)
            return Result.Failure<string>(DomainErrorCode.Tenant.ContextMissing);

        var dbContext = (DbContext)_unitOfWork;

        var plan = await dbContext.Set<SubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive, cancellationToken);

        if (plan == null)
            return Result.Failure<string>(DomainErrorCode.Subscription.PlanNotFound);

        if (string.IsNullOrWhiteSpace(plan.StripePriceId))
            return Result.Failure<string>(DomainErrorCode.Subscription.PlanNotBillable);

        var subscription = await dbContext.Set<TenantSubscription>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value, cancellationToken);

        if (subscription == null)
            return Result.Failure<string>(DomainErrorCode.Subscription.NotFound);

        // Use stored StripeCustomerId or create a new one
        if (string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
        {
            var tenant = await dbContext.Set<Tenant>()
                .FirstOrDefaultAsync(t => t.Id == tenantId.Value, cancellationToken);

            var user = await dbContext.Set<User>()
                .FirstOrDefaultAsync(u => u.TenantId == tenantId.Value, cancellationToken);

            var customerEmail = user!.Email;
            var tenantName = tenant!.BusinessName;

            var stripeCustomerId = await _stripeService.CreateCustomerAsync(customerEmail, tenantName, cancellationToken);
            subscription.SetStripeCustomerId(stripeCustomerId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var checkoutUrl = await _stripeService.CreateCheckoutSessionAsync(
            subscription.StripeCustomerId!,
            plan.StripePriceId,
            tenantId.Value,
            plan.TrialDays,
            request.SuccessUrl,
            request.CancelUrl,
            cancellationToken);

        _logger.LogInformation("Checkout session created for Tenant {TenantId}, Plan {PlanName}", tenantId, plan.Name);

        return Result.Success(checkoutUrl);
    }
}
