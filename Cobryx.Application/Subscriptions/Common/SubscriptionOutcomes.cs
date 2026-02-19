using Cobryx.Domain.Common;

namespace Cobryx.Application.Subscriptions.Common;

public static class SubscriptionOutcomes
{
    private const string Prefix = "BILLING.SUBSCRIPTION";

    public static readonly Outcome Status = new($"{Prefix}.STATUS_CHECK_SUCCESS", OutcomeCategory.Success, "Subscription status retrieved successfully.");
    public static readonly Outcome SyncAuthoritative = new($"{Prefix}.SYNC_SUCCESS", OutcomeCategory.Success, "Subscription synchronization successful.");
    public static readonly Outcome Intelligence = new($"{Prefix}.INTELLIGENCE_SUCCESS", OutcomeCategory.Success, "Plan intelligence retrieved successfully.");
    public static readonly Outcome CheckoutCreated = new($"{Prefix}.CHECKOUT_CREATE_SUCCESS", OutcomeCategory.Success, "Checkout session created successfully.");
    public static readonly Outcome CheckoutFailed = new($"{Prefix}.CHECKOUT_CREATE_FAILED", OutcomeCategory.BusinessError, "Checkout session creation failed.");
    public static readonly Outcome PortalCreated = new($"{Prefix}.PORTAL_CREATE_SUCCESS", OutcomeCategory.Success, "Customer portal session created successfully.");
    public static readonly Outcome Plans = new($"{Prefix}.PLANS_FETCH_SUCCESS", OutcomeCategory.Success, "Subscription plans retrieved successfully.");
    public static readonly Outcome NoStripeCustomer = new($"{Prefix}.NO_STRIPE_CUSTOMER_ERROR", OutcomeCategory.BusinessError, "No Stripe customer found.");
    public static readonly Outcome NotFound = new($"{Prefix}.NOT_FOUND", OutcomeCategory.BusinessError, "Subscription not found.");
    public static readonly Outcome PlanNotFound = new($"{Prefix}.PLAN_NOT_FOUND", OutcomeCategory.BusinessError, "Subscription plan not found.");
    public static readonly Outcome PlanNotBillable = new($"{Prefix}.PLAN_NOT_BILLABLE", OutcomeCategory.BusinessError, "Subscription plan is not billable.");
    public static readonly Outcome TenantNotFound = new($"{Prefix}.TENANT_NOT_FOUND", OutcomeCategory.BusinessError, "Tenant not found.");
}
