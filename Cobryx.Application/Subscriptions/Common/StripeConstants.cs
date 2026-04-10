namespace Cobryx.Application.Subscriptions.Common;

/// <summary>
/// Centralized Stripe-related constants to eliminate magic strings in the billing module.
/// </summary>
public static class StripeConstants
{
    public static class Events
    {
        public const string CheckoutSessionCompleted = "checkout.session.completed";
        public const string InvoicePaid = "invoice.paid";
        public const string InvoicePaymentFailed = "invoice.payment_failed";
        public const string SubscriptionUpdated = "customer.subscription.updated";
        public const string SubscriptionDeleted = "customer.subscription.deleted";
        public const string PaymentIntentSucceeded = "payment_intent.succeeded";
        public const string PaymentIntentFailed = "payment_intent.payment_failed";
        public const string ChargeRefunded = "charge.refunded";
        public const string AccountUpdated = "account.updated";
        public const string PayoutPaid = "payout.paid";
        public const string PayoutFailed = "payout.failed";
        public const string ManualSyncRequested = "manual.sync.requested";
    }

    public static class Statuses
    {
        public const string Trialing = "trialing";
        public const string Active = "active";
        public const string PastDue = "past_due";
        public const string Canceled = "canceled";
        public const string Unpaid = "unpaid";
    }

    public static class PaymentIntentStatuses
    {
        public const string Succeeded = "succeeded";
        public const string Processing = "processing";
        public const string RequiresPaymentMethod = "requires_payment_method";
        public const string RequiresConfirmation = "requires_confirmation";
        public const string RequiresAction = "requires_action";
        public const string Canceled = "canceled";
    }
}
