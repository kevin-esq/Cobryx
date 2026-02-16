using Asp.Versioning;
using Cobryx.Api.Contracts.V1.Subscriptions;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Outcomes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Subscriptions.Commands.CreateCheckoutSession;
using Cobryx.Application.Subscriptions.Services;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Cobryx.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/subscription")]
[Tags("Subscription")]
public class SubscriptionController : CobryxBaseController
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeService _stripeService;

    public SubscriptionController(
        ISender sender,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        IStripeService stripeService) : base(sender)
    {
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _stripeService = stripeService;
    }

    /// <summary>
    /// Gets the current tenant subscription status and plan details.
    /// </summary>
    /// <response code="200">The subscription status and plan details.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponse<SubscriptionStatusDto>), 200)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var dbContext = (DbContext)_unitOfWork;
        var subscription = await dbContext.Set<TenantSubscription>()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value, ct);

        if (subscription == null)
            return NotFound();

        var dto = new SubscriptionStatusDto(
            subscription.Status.ToString(),
            subscription.Plan?.Name ?? CobryxDefaults.UnknownValue,
            subscription.Plan?.Tier.ToString() ?? "Free",
            subscription.Plan?.MaxInvoices ?? 0,
            subscription.Plan?.MaxUsers ?? 0,
            subscription.TrialEndsAtUtc,
            subscription.GracePeriodEndsAtUtc,
            subscription.StripeCustomerId != null);

        return Ok(ApiResponseFactory.Success(dto, SubscriptionOutcomes.Status));
    }

    /// <summary>
    /// Creates a Stripe Checkout session for upgrading the subscription.
    /// </summary>
    /// <param name="request">The checkout details including plan and optional return URLs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// Returns a URL to redirect the user to Stripe Checkout. The user will be returned to SuccessUrl or CancelUrl after completion.
    /// 
    /// Possible Outcomes:
    /// - SUBSCRIPTION.CHECKOUT.CREATED: Session successfully created, Stripe URL returned.
    /// - SUBSCRIPTION.CHECKOUT.FAILED: General failure in Stripe communication.
    /// </remarks>
    /// <response code="200">The checkout URL.</response>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(ApiSuccessResponse<CheckoutUrlDto>), 200)]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutRequest request, CancellationToken ct)
    {
        var command = new CreateCheckoutSessionCommand(request.PlanId, request.SuccessUrl, request.CancelUrl);
        var result = await Sender.Send(command, ct);

        if (!result.IsSuccess)
            return BadRequest(ApiResponseFactory.Error(result.Error ?? SubscriptionOutcomes.CheckoutFailed));

        return Ok(ApiResponseFactory.Success(new CheckoutUrlDto(result.Value!), SubscriptionOutcomes.CheckoutCreated));
    }

    /// <summary>
    /// Creates a Stripe Billing Portal session for managing the subscription.
    /// </summary>
    /// <param name="request">Optional return URL after leaving the portal.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// Returns a URL to redirect the user to Stripe's self-service portal. The user will be returned to ReturnUrl after leaving the portal.
    /// 
    /// Possible Outcomes:
    /// - SUBSCRIPTION.PORTAL.CREATED: Session successfully created, portal URL returned.
    /// - SUBSCRIPTION.PORTAL.FAILED: User has no active Stripe customer or subscription.
    /// </remarks>
    /// <response code="200">The portal URL.</response>
    [HttpPost("portal")]
    [ProducesResponseType(typeof(ApiSuccessResponse<CheckoutUrlDto>), 200)]
    public async Task<IActionResult> CreatePortal([FromBody] CreatePortalRequest request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Unauthorized();

        var dbContext = (DbContext)_unitOfWork;
        var subscription = await dbContext.Set<TenantSubscription>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value, ct);

        if (subscription?.StripeCustomerId == null)
            return BadRequest(ApiResponseFactory.Error(SubscriptionOutcomes.NoStripeCustomer));

        var portalUrl = await _stripeService.CreateBillingPortalSessionAsync(subscription.StripeCustomerId, request.ReturnUrl, ct);

        return Ok(ApiResponseFactory.Success(new CheckoutUrlDto(portalUrl), SubscriptionOutcomes.PortalCreated));
    }

    /// <summary>
    /// Lists all available subscription plans.
    /// </summary>
    /// <response code="200">A collection of available subscription plans.</response>
    [HttpGet("plans")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<PlanDto>>), 200)]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
    {
        var dbContext = (DbContext)_unitOfWork;
        var plans = await dbContext.Set<SubscriptionPlan>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Tier)
            .Select(p => new PlanDto(p.Id, p.Name, p.Description, p.Price.Amount, p.Price.Currency, p.MaxInvoices, p.MaxUsers, p.Tier.ToString(), p.TrialDays))
            .ToListAsync(ct);

        return Ok(ApiResponseFactory.Success(plans, SubscriptionOutcomes.Plans));
    }
}
