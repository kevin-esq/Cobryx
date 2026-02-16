using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Subscriptions.Services;

/// <summary>
/// Idempotent sync service for Stripe subscription webhook events.
/// Handles out-of-order delivery, duplicates, and late arrivals.
/// Stripe is the source of truth — never invent local state.
/// </summary>
public class StripeSubscriptionSyncService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeService _stripeService;
    private readonly ILogger<StripeSubscriptionSyncService> _logger;

    public StripeSubscriptionSyncService(
        IUnitOfWork unitOfWork,
        IStripeService stripeService,
        ILogger<StripeSubscriptionSyncService> logger)
    {
        _unitOfWork = unitOfWork;
        _stripeService = stripeService;
        _logger = logger;
    }

    private DbContext Db => (DbContext)_unitOfWork;

    /// <summary>
    /// Handles checkout.session.completed: store Stripe IDs and sync subscription state.
    /// </summary>
    public async Task HandleCheckoutCompletedAsync(
        string stripeCustomerId,
        string stripeSubscriptionId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var subscription = await GetSubscriptionAsync(tenantId, ct);
        if (subscription == null)
        {
            _logger.LogWarning("No TenantSubscription found for Tenant {TenantId} during checkout sync", tenantId);
            return;
        }

        // Store the Stripe Customer ID if not already set
        if (string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
            subscription.SetStripeCustomerId(stripeCustomerId);

        // Fetch current state from Stripe (source of truth)
        var stripeState = await _stripeService.GetSubscriptionStateAsync(stripeSubscriptionId, ct);
        var status = MapStripeStatus(stripeState.Status);

        // Resolve PlanId from StripePriceId
        var plan = await Db.Set<SubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.StripePriceId == stripeState.PriceId, ct);

        var planId = plan?.Id ?? subscription.PlanId;

        subscription.SyncFromStripe(stripeSubscriptionId, status, stripeState.TrialEnd, planId);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Checkout synced for Tenant {TenantId}: Status={Status}, SubscriptionId={StripeSubscriptionId}",
            tenantId, status, stripeSubscriptionId);
    }

    /// <summary>
    /// Handles invoice.paid: activate subscription after successful payment.
    /// </summary>
    public async Task HandleInvoicePaidAsync(string stripeSubscriptionId, CancellationToken ct = default)
    {
        var subscription = await GetSubscriptionByStripeIdAsync(stripeSubscriptionId, ct);
        if (subscription == null)
        {
            _logger.LogWarning("No subscription found for Stripe SubscriptionId {StripeSubscriptionId}", stripeSubscriptionId);
            return;
        }

        subscription.ActivateFromPayment();
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Subscription activated for Tenant {TenantId} via invoice.paid", subscription.TenantId);
    }

    /// <summary>
    /// Handles invoice.payment_failed: mark as PastDue.
    /// </summary>
    public async Task HandleInvoicePaymentFailedAsync(string stripeSubscriptionId, CancellationToken ct = default)
    {
        var subscription = await GetSubscriptionByStripeIdAsync(stripeSubscriptionId, ct);
        if (subscription == null) return;

        subscription.HandlePaymentFailed();
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogWarning("Payment failed for Tenant {TenantId}, status set to PastDue", subscription.TenantId);
    }

    /// <summary>
    /// Handles customer.subscription.updated: sync status from Stripe.
    /// </summary>
    public async Task HandleSubscriptionUpdatedAsync(string stripeSubscriptionId, CancellationToken ct = default)
    {
        var subscription = await GetSubscriptionByStripeIdAsync(stripeSubscriptionId, ct);
        if (subscription == null) return;

        var stripeState = await _stripeService.GetSubscriptionStateAsync(stripeSubscriptionId, ct);
        var status = MapStripeStatus(stripeState.Status);

        var plan = await Db.Set<SubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.StripePriceId == stripeState.PriceId, ct);

        subscription.SyncFromStripe(stripeSubscriptionId, status, stripeState.TrialEnd, plan?.Id ?? subscription.PlanId);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Subscription updated for Tenant {TenantId}: Status={Status}", subscription.TenantId, status);
    }

    /// <summary>
    /// Handles customer.subscription.deleted: cancel with grace period.
    /// </summary>
    public async Task HandleSubscriptionDeletedAsync(string stripeSubscriptionId, CancellationToken ct = default)
    {
        var subscription = await GetSubscriptionByStripeIdAsync(stripeSubscriptionId, ct);
        if (subscription == null) return;

        var gracePeriodEnd = DateTime.UtcNow.AddDays(7);
        subscription.ExecuteCancellation(gracePeriodEnd, CancellationReason.Other);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Subscription deleted for Tenant {TenantId}, grace period until {GracePeriodEnd}",
            subscription.TenantId, gracePeriodEnd);
    }

    private async Task<TenantSubscription?> GetSubscriptionAsync(Guid tenantId, CancellationToken ct)
    {
        return await Db.Set<TenantSubscription>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
    }

    private async Task<TenantSubscription?> GetSubscriptionByStripeIdAsync(string stripeSubscriptionId, CancellationToken ct)
    {
        return await Db.Set<TenantSubscription>()
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, ct);
    }

    private static SubscriptionStatus MapStripeStatus(string stripeStatus) => stripeStatus switch
    {
        "trialing" => SubscriptionStatus.Trial,
        "active" => SubscriptionStatus.Active,
        "past_due" => SubscriptionStatus.PastDue,
        "canceled" => SubscriptionStatus.Cancelled,
        "unpaid" => SubscriptionStatus.PastDue,
        _ => SubscriptionStatus.Active
    };
}
