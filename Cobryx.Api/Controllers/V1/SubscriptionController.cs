using Asp.Versioning;

using Cobryx.Api.Infrastructure;
using Cobryx.Application.Subscriptions.Commands.CreateCheckoutSession;
using Cobryx.Application.Subscriptions.Commands.CreatePortalSession;
using Cobryx.Application.Subscriptions.Commands.SyncSubscription;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Application.Subscriptions.Queries.GetSubscriptionPlans;
using Cobryx.Application.Subscriptions.Queries.GetSubscriptionStatus;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/subscription")]
[Tags("Subscription")]
public class SubscriptionController : CobryxBaseController
{
    public SubscriptionController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Retrieves all available subscription plans.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - BILLING.SUBSCRIPTION.PLANS_FETCH_SUCCESS: Plans retrieved successfully.
    /// </remarks>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    [HttpGet("plans")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<PlanDto>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
    {
        var result = await Sender.Send(new GetSubscriptionPlansQuery(), ct);
        return HandleResult(result, SubscriptionOutcomes.Plans);
    }

    /// <summary>
    /// Retrieves the current tenant's subscription status and entitlement details.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - BILLING.SUBSCRIPTION.STATUS_CHECK_SUCCESS: Status retrieved successfully.
    /// - BILLING.SUBSCRIPTION.NOT_FOUND: Tenant has no active subscription.
    /// </remarks>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiSuccessResponse<SubscriptionStatusDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await Sender.Send(new GetSubscriptionStatusQuery(), ct);
        return HandleResult(result, SubscriptionOutcomes.Status);
    }

    /// <summary>
    /// Retrieves advanced plan intelligence including resource usage snapshots,
    /// capability flags, and upgrade recommendations to drive conversion UX.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - BILLING.SUBSCRIPTION.INTELLIGENCE_FETCH_SUCCESS: Intelligence retrieved successfully.
    /// </remarks>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    [HttpGet("intelligence")]
    [ProducesResponseType(typeof(ApiSuccessResponse<SubscriptionIntelligenceDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> GetIntelligence(CancellationToken ct)
    {
        var result = await Sender.Send(new Cobryx.Application.Subscriptions.Queries.GetSubscriptionIntelligence.GetSubscriptionIntelligenceQuery(), ct);
        return HandleResult(result, SubscriptionOutcomes.Intelligence);
    }

    /// <summary>
    /// Initiates a Stripe Checkout session for plan upgrade or initial purchase.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - BILLING.SUBSCRIPTION.CHECKOUT_CREATE_SUCCESS: URL generated successfully.
    /// - BILLING.SUBSCRIPTION.CHECKOUT_CREATE_FAILED: Failed to communicate with Stripe.
    /// </remarks>
    /// <param name="request">Plan details and redirect URLs.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    [HttpPost("checkout")]
    [AllowExpiredSubscription]
    [ProducesResponseType(typeof(ApiSuccessResponse<CheckoutUrlResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutSessionRequest request, CancellationToken ct)
    {
        var command = new CreateCheckoutSessionCommand(request.PlanId, request.SuccessUrl, request.CancelUrl);
        var result = await Sender.Send(command, ct);

        var mappedResult = result.IsSuccess
            ? Result.Success(new CheckoutUrlResponse(result.Value!))
            : Result.Failure<CheckoutUrlResponse>(result.Error!);

        return HandleResult(mappedResult, SubscriptionOutcomes.CheckoutCreated);
    }

    /// <summary>
    /// Generates a link to the Stripe Customer Portal for subscription management.
    /// </summary>
    /// <remarks>
    /// Possible Outcomes:
    /// - BILLING.SUBSCRIPTION.PORTAL_CREATE_SUCCESS: Portal session created.
    /// - BILLING.SUBSCRIPTION.NO_STRIPE_CUSTOMER_ERROR: The tenant has no associated Stripe customer record.
    /// </remarks>
    /// <param name="request">The return URL after the user leaves the portal.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    [HttpPost("portal")]
    [AllowExpiredSubscription]
    [ProducesResponseType(typeof(ApiSuccessResponse<CheckoutUrlResponse>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> CreatePortal([FromBody] CreatePortalSessionRequest request, CancellationToken ct)
    {
        var result = await Sender.Send(new CreatePortalSessionCommand(request.ReturnUrl), ct);

        var mappedResult = result.IsSuccess
            ? Result.Success(new CheckoutUrlResponse(result.Value!))
            : Result.Failure<CheckoutUrlResponse>(result.Error!);

        return HandleResult(mappedResult, SubscriptionOutcomes.PortalCreated);
    }

    /// <summary>
    /// Forced authoritative synchronization of the tenant's subscription state from Stripe.
    /// Use this as a recovery mechanism if webhooks are delayed or missed.
    /// </summary>
    /// <remarks>
    /// Access Policy: Restricted to users with 'CanManageTenant' administrative permissions.
    /// Implementation: Performs a tenant-level row lock (FOR UPDATE) to prevent concurrency issues.
    ///
    /// Possible Outcomes:
    /// - BILLING.SUBSCRIPTION.SYNC_SUCCESS: Local state updated from Stripe.
    /// </remarks>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    [HttpPost("sync")]
    [Authorize(Policy = "CanManageTenant")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> Sync(CancellationToken ct)
    {
        var result = await Sender.Send(new SyncSubscriptionCommand(), ct);
        return HandleResult(result, SubscriptionOutcomes.SyncAuthoritative);
    }
}
