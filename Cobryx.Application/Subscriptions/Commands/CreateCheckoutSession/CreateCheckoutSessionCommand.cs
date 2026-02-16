using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Common;
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
        ILogger<CreateCheckoutSessionHandler> _logger)
    {
        _unitOfWork = unitOfWork;
        _stripeService = stripeService;
        _tenantProvider = tenantProvider;
        this._logger = _logger;
    }

    public async Task<Result<string>> Handle(CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null)
            return Result.Failure<string>("SUBSCRIPTION.TENANT_NOT_FOUND");

        var dbContext = (DbContext)_unitOfWork;

        var plan = await dbContext.Set<SubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive, cancellationToken);

        if (plan == null)
            return Result.Failure<string>("SUBSCRIPTION.PLAN_NOT_FOUND");

        if (string.IsNullOrWhiteSpace(plan.StripePriceId))
            return Result.Failure<string>("SUBSCRIPTION.PLAN_NOT_BILLABLE");

        var subscription = await dbContext.Set<TenantSubscription>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value, cancellationToken);

        if (subscription == null)
            return Result.Failure<string>("SUBSCRIPTION.NOT_FOUND");

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
