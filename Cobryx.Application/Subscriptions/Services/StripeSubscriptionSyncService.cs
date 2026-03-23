using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Subscriptions.Services;

/// <summary>
/// Orchestrates the synchronization of subscription states between Stripe (Source of Truth) and the local database.
/// </summary>
/// <remarks>
/// Design Principles:
/// 1. Authoritative State: We never "calculate" the next state locally. We always fetch the latest subscription
///    and price details from Stripe once a webhook event signals a change.
/// 2. Idempotency: Uses the 'ProcessedStripeEvents' table to ensure every Stripe event ID is handled exactly once.
/// 3. Cross-Provider Integrity: Detects database-level race conditions to support concurrent webhook deliveries.
/// </remarks>
public class StripeSubscriptionSyncService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeService _stripeService;
    private readonly IClock _clock;
    private readonly ICacheService _cacheService;
    private readonly IGrowthIntelligenceService _growthService;
    private readonly ILogger<StripeSubscriptionSyncService> _logger;

    public StripeSubscriptionSyncService(
        IUnitOfWork unitOfWork,
        IStripeService stripeService,
        IClock clock,
        ICacheService cacheService,
        IGrowthIntelligenceService growthService,
        ILogger<StripeSubscriptionSyncService> logger)
    {
        _unitOfWork = unitOfWork;
        _stripeService = stripeService;
        _clock = clock;
        _cacheService = cacheService;
        _growthService = growthService;
        _logger = logger;
    }

    private DbContext Db => (DbContext)_unitOfWork;

    /// <summary>
    /// Handles checkout.session.completed: store Stripe IDs and sync subscription state.
    /// </summary>
    public async Task HandleCheckoutCompletedAsync(
        string stripeEventId,
        string stripeCustomerId,
        string stripeSubscriptionId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        if (await IsAlreadyProcessedAsync(stripeEventId, ct))
            return;
        var subscription = await GetSubscriptionAsync(tenantId, ct);
        if (subscription == null)
        {
            _logger.LogWarning("No TenantSubscription found for Tenant {TenantId} during checkout sync", tenantId);
            return;
        }

        // Store the Stripe Customer ID if not already set (Lazy association)
        if (string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
            subscription.SetStripeCustomerId(stripeCustomerId);

        await SyncAuthoritativeStateAsync(stripeEventId, stripeSubscriptionId, subscription, StripeConstants.Events.CheckoutSessionCompleted, ct);
    }

    /// <summary>
    /// Handles invoice.paid: activate subscription after successful payment.
    /// </summary>
    public async Task HandleInvoicePaidAsync(string stripeEventId, string stripeSubscriptionId, CancellationToken ct = default)
    {
        if (await IsAlreadyProcessedAsync(stripeEventId, ct))
            return;
        var subscription = await GetSubscriptionByStripeIdAsync(stripeSubscriptionId, ct);
        if (subscription == null)
        {
            _logger.LogWarning("No subscription found for Stripe SubscriptionId {StripeSubscriptionId} during invoice.paid", stripeSubscriptionId);
            return;
        }

        await SyncAuthoritativeStateAsync(stripeEventId, stripeSubscriptionId, subscription, StripeConstants.Events.InvoicePaid, ct);
    }

    /// <summary>
    /// Handles invoice.payment_failed: mark as PastDue.
    /// </summary>
    public async Task HandleInvoicePaymentFailedAsync(string stripeEventId, string stripeSubscriptionId, CancellationToken ct = default)
    {
        if (await IsAlreadyProcessedAsync(stripeEventId, ct))
            return;
        var subscription = await GetSubscriptionByStripeIdAsync(stripeSubscriptionId, ct);
        if (subscription == null)
            return;

        await SyncAuthoritativeStateAsync(stripeEventId, stripeSubscriptionId, subscription, StripeConstants.Events.InvoicePaymentFailed, ct);
    }

    /// <summary>
    /// Handles customer.subscription.updated: sync status from Stripe.
    /// </summary>
    public async Task HandleSubscriptionUpdatedAsync(string stripeEventId, string stripeSubscriptionId, CancellationToken ct = default)
    {
        if (await IsAlreadyProcessedAsync(stripeEventId, ct))
            return;
        var subscription = await GetSubscriptionByStripeIdAsync(stripeSubscriptionId, ct);
        if (subscription == null)
            return;

        await SyncAuthoritativeStateAsync(stripeEventId, stripeSubscriptionId, subscription, StripeConstants.Events.SubscriptionUpdated, ct);
    }

    /// <summary>
    /// Handles customer.subscription.deleted: cancel with grace period.
    /// </summary>
    public async Task HandleSubscriptionDeletedAsync(string stripeEventId, string stripeSubscriptionId, CancellationToken ct = default)
    {
        if (await IsAlreadyProcessedAsync(stripeEventId, ct))
            return;
        var subscription = await GetSubscriptionByStripeIdAsync(stripeSubscriptionId, ct);
        if (subscription == null)
            return;

        await SyncAuthoritativeStateAsync(stripeEventId, stripeSubscriptionId, subscription, StripeConstants.Events.SubscriptionDeleted, ct);
    }

    /// <summary>
    /// Executes the authoritative synchronization by fetching the latest state from Stripe.
    /// This pattern gracefully handles delayed or out-of-order webhook delivery.
    /// </summary>
    public async Task SyncAuthoritativeStateAsync(
        string stripeEventId,
        string stripeSubscriptionId,
        TenantSubscription subscription,
        string eventType,
        CancellationToken ct)
    {
        var oldStatus = subscription.Status;

        // Fetch current state from Stripe (source of truth)
        var stripeState = await _stripeService.GetSubscriptionStateAsync(stripeSubscriptionId, ct);
        var status = MapStripeStatus(stripeState.Status);

        // Resolve PlanId from StripePriceId
        var plan = await Db.Set<SubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.StripePriceId == stripeState.PriceId, ct);

        var newPlanId = plan?.Id ?? subscription.PlanId;

        IDbContextTransaction? transaction = null;
        if (Db.Database.CurrentTransaction == null && Db.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            transaction = await Db.Database.BeginTransactionAsync(ct);
        }

        try
        {
            subscription.SyncFromStripe(stripeSubscriptionId, status, stripeState.TrialEnd, newPlanId, _clock.UtcNow);

            Db.Set<ProcessedStripeEvent>().Add(new ProcessedStripeEvent(stripeEventId, eventType, _clock.UtcNow));
            await _unitOfWork.SaveChangesAsync(ct);

            try
            {
                await _cacheService.RemoveAsync($"subscription_access:{subscription.TenantId}", ct);
            }
            catch (Exception cacheEx)
            {
                _logger.LogWarning(cacheEx, "Failed to invalidate subscription cache for Tenant {TenantId}", subscription.TenantId);
            }

            _logger.LogInformation(
                "Subscription synced for Tenant {TenantId}. {OldStatus} -> {NewStatus}. Event: {EventType}, EventId: {EventId}",
                subscription.TenantId, oldStatus, subscription.Status, eventType, stripeEventId);

            var mrrChangeType = MRRChangeType.None;
            var newMrr = plan?.Price.Amount ?? 0;

            if (subscription.Status == SubscriptionStatus.Active)
            {
                if (oldStatus == SubscriptionStatus.Trial || oldStatus == SubscriptionStatus.Active)
                {
                    if (plan != null && (subscription.Plan == null || plan.Id != subscription.Plan.Id))
                    {
                        var oldPrice = subscription.Plan?.Price.Amount ?? 0;
                        mrrChangeType = newMrr > oldPrice ? MRRChangeType.Expansion : MRRChangeType.Contraction;
                    }
                    else if (oldStatus == SubscriptionStatus.Trial)
                    {
                        mrrChangeType = MRRChangeType.New;
                    }
                }
            }
            else if (subscription.Status == SubscriptionStatus.Cancelled && oldStatus != SubscriptionStatus.Cancelled)
            {
                mrrChangeType = MRRChangeType.Churn;
                newMrr = 0;
            }

            if (mrrChangeType != MRRChangeType.None)
            {
                await _growthService.RecordMRRTransitionAsync(subscription.TenantId, newMrr, mrrChangeType, eventType);
            }

            if (transaction != null)
                await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            if (transaction != null)
                await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to sync authoritative state for Subscription {StripeSubscriptionId}. Rollback occurred.", stripeSubscriptionId);
            throw;
        }

        // Bust subscription gate cache immediately after state change (Outside transaction to allow partial success of cache invalidation)
    }

    private async Task<TenantSubscription?> GetSubscriptionAsync(Guid tenantId, CancellationToken ct)
    {
        return await Db.Set<TenantSubscription>()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
    }

    private async Task<TenantSubscription?> GetSubscriptionByStripeIdAsync(string stripeSubscriptionId, CancellationToken ct)
    {
        return await Db.Set<TenantSubscription>()
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, ct);
    }

    private async Task<bool> IsAlreadyProcessedAsync(string eventId, CancellationToken ct)
    {
        return await Db.Set<ProcessedStripeEvent>()
            .AnyAsync(e => e.StripeEventId == eventId, ct);
    }

    private static SubscriptionStatus MapStripeStatus(string stripeStatus) => stripeStatus switch
    {
        StripeConstants.Statuses.Trialing => SubscriptionStatus.Trial,
        StripeConstants.Statuses.Active => SubscriptionStatus.Active,
        StripeConstants.Statuses.PastDue => SubscriptionStatus.PastDue,
        StripeConstants.Statuses.Canceled => SubscriptionStatus.Cancelled,
        StripeConstants.Statuses.Unpaid => SubscriptionStatus.PastDue,
        _ => SubscriptionStatus.Active
    };
}
