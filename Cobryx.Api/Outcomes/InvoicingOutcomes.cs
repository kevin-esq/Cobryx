using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

/// <summary>
/// Status codes for the Invoicing domain operations (Contract Level).
/// </summary>
public static class InvoicingOutcomes
{
    public static class Taxes
    {
        private const string Prefix = "BILLING.TAX";
        public static readonly Outcome Created = new($"{Prefix}.CREATE_SUCCESS", OutcomeCategory.Success, "Tax configuration created successfully.");
        public static readonly Outcome Updated = new($"{Prefix}.UPDATE_SUCCESS", OutcomeCategory.Success, "Tax configuration updated successfully.");
        public static readonly Outcome Deleted = new($"{Prefix}.DELETE_SUCCESS", OutcomeCategory.Success, "Tax configuration deleted successfully.");
        public static readonly Outcome SearchCompleted = new($"{Prefix}.SEARCH_SUCCESS", OutcomeCategory.Success, "Tax search completed.");

        public static readonly Outcome ValidationFailed = new($"{Prefix}.VALIDATION_FAILED", OutcomeCategory.BusinessError, "Tax validation failed.");
        public static readonly Outcome Conflict = new($"{Prefix}.CONFLICT_ERROR", OutcomeCategory.BusinessError, "Tax conflict occurred.");
    }

    public static class PaymentMethods
    {
        private const string Prefix = "BILLING.PAYMENT_METHOD";
        public static readonly Outcome SearchCompleted = new($"{Prefix}.SEARCH_SUCCESS", OutcomeCategory.Success, "Payment method search completed.");
        public static readonly Outcome Created = new($"{Prefix}.CREATE_SUCCESS", OutcomeCategory.Success, "Payment method created successfully.");
        public static readonly Outcome Updated = new($"{Prefix}.UPDATE_SUCCESS", OutcomeCategory.Success, "Payment method updated successfully.");
        public static readonly Outcome Deleted = new($"{Prefix}.DELETE_SUCCESS", OutcomeCategory.Success, "Payment method deleted successfully.");

        public static readonly Outcome ValidationFailed = new($"{Prefix}.VALIDATION_FAILED", OutcomeCategory.BusinessError, "Payment method validation failed.");
    }

    public static class Invoices
    {
        private const string Prefix = "BILLING.INVOICE";
        public static readonly Outcome SearchCompleted = new($"{Prefix}.SEARCH_SUCCESS", OutcomeCategory.Success, "Invoice search completed.");
        public static readonly Outcome Created = new($"{Prefix}.CREATE_SUCCESS", OutcomeCategory.Success, "Invoice created successfully.");
        public static readonly Outcome Updated = new($"{Prefix}.UPDATE_SUCCESS", OutcomeCategory.Success, "Invoice updated successfully.");
        public static readonly Outcome Deleted = new($"{Prefix}.DELETE_SUCCESS", OutcomeCategory.Success, "Invoice deleted successfully.");
        public static readonly Outcome Issued = new($"{Prefix}.ISSUE_SUCCESS", OutcomeCategory.Success, "Invoice issued successfully.");
        public static readonly Outcome Paid = new($"{Prefix}.PAYMENT_SUCCESS", OutcomeCategory.Success, "Invoice paid successfully.");
        public static readonly Outcome PaymentApplied = new($"{Prefix}.ALLOCATION_SUCCESS", OutcomeCategory.Success, "Payment applied to invoice successfully.");
        public static readonly Outcome Cancelled = new($"{Prefix}.CANCEL_SUCCESS", OutcomeCategory.Success, "Invoice cancelled successfully.");
        public static readonly Outcome Recalculated = new($"{Prefix}.RECALCULATE_SUCCESS", OutcomeCategory.Success, "Invoice recalculated.");
        public static readonly Outcome StatusUpdated = new($"{Prefix}.STATUS_UPDATE_SUCCESS", OutcomeCategory.Success, "Invoice status updated.");

        public static readonly Outcome ValidationFailed = new($"{Prefix}.VALIDATION_FAILED", OutcomeCategory.BusinessError, "Invoice validation failed.");
        public static readonly Outcome Conflict = new($"{Prefix}.CONFLICT_ERROR", OutcomeCategory.BusinessError, "Invoice conflict occurred.");
    }

    public static class Payments
    {
        private const string Prefix = "BILLING.PAYMENT";
        public static readonly Outcome Initiated = new($"{Prefix}.INTENT_CREATED", OutcomeCategory.Success, "Payment intent created.");
        public static readonly Outcome Completed = new($"{Prefix}.PROCESS_SUCCESS", OutcomeCategory.Success, "Payment completed successfully.");
        public static readonly Outcome Refunded = new($"{Prefix}.REFUND_SUCCESS", OutcomeCategory.Success, "Payment refunded successfully.");
        public static readonly Outcome Retrying = new($"{Prefix}.RETRY_INITIATED", OutcomeCategory.Success, "Payment retry initiated.");
        public static readonly Outcome Failed = new($"{Prefix}.PROCESS_FAILED", OutcomeCategory.BusinessError, "Payment failed.");
    }

    public static class Webhooks
    {
        private const string Prefix = "FINANCIAL.WEBHOOK";
        public static readonly Outcome Received = new($"{Prefix}.RECEIVED", OutcomeCategory.Info, "Webhook received.");
        public static readonly Outcome Deduped = new($"{Prefix}.DEDUPED", OutcomeCategory.Info, "Webhook deduped.");
        public static readonly Outcome Processed = new($"{Prefix}.PROCESSED", OutcomeCategory.Success, "Webhook processed successfully.");
        public static readonly Outcome Failed = new($"{Prefix}.FAILED", OutcomeCategory.BusinessError, "Webhook processing failed.");
    }
}
