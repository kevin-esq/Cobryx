namespace Cobryx.Api.Outcomes;

public static class SubscriptionOutcomes
{
    private const string Prefix = "BILLING.SUBSCRIPTION";

    public const string Status = $"{Prefix}.STATUS_CHECK_SUCCESS";
    public const string CheckoutCreated = $"{Prefix}.CHECKOUT_CREATE_SUCCESS";
    public const string CheckoutFailed = $"{Prefix}.CHECKOUT_CREATE_FAILED";
    public const string PortalCreated = $"{Prefix}.PORTAL_CREATE_SUCCESS";
    public const string Plans = $"{Prefix}.PLANS_FETCH_SUCCESS";
    public const string NoStripeCustomer = $"{Prefix}.NO_STRIPE_CUSTOMER_ERROR";
}
