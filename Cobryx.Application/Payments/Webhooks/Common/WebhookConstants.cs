namespace Cobryx.Application.Payments.Webhooks.Common;

/// <summary>
/// Machine-readable keys for internal webhook event routing, decoupling infrastructure types from application logic.
/// </summary>
public static class WebhookConstants
{
    public static class InternalEvents
    {
        public const string PaymentSucceeded = "PaymentSucceeded";
        public const string PaymentFailed = "PaymentFailed";
        public const string CheckoutCompleted = "CheckoutCompleted";
        public const string InvoicePaid = "InvoicePaid";
        public const string InvoicePaymentFailed = "InvoicePaymentFailed";
        public const string SubscriptionUpdated = "SubscriptionUpdated";
        public const string SubscriptionDeleted = "SubscriptionDeleted";
        public const string ChargeRefunded = "ChargeRefunded";
        public const string ChargeDisputeCreated = "ChargeDisputeCreated";
        public const string PayoutPaid = "PayoutPaid";
        public const string PayoutFailed = "PayoutFailed";
    }
}
