namespace Cobryx.Api.Outcomes;

public static class InvoicingOutcomes
{
    public static class Taxes
    {
        private const string Prefix = "BILLING.TAX";
        public const string SearchCompleted = $"{Prefix}.SEARCH_SUCCESS";
        public const string Created = $"{Prefix}.CREATE_SUCCESS";
        public const string Deleted = $"{Prefix}.DELETE_SUCCESS";

        public const string ValidationFailed = $"{Prefix}.VALIDATION_FAILED";
        public const string Conflict = $"{Prefix}.CONFLICT_ERROR";
    }

    public static class PaymentMethods
    {
        private const string Prefix = "BILLING.PAYMENT_METHOD";
        public const string SearchCompleted = $"{Prefix}.SEARCH_SUCCESS";
        public const string Created = $"{Prefix}.CREATE_SUCCESS";
        public const string Deleted = $"{Prefix}.DELETE_SUCCESS";
    }

    public static class Invoices
    {
        private const string Prefix = "BILLING.INVOICE";
        public const string SearchCompleted = $"{Prefix}.SEARCH_SUCCESS";
        public const string Created = $"{Prefix}.CREATE_SUCCESS";
        public const string Issued = $"{Prefix}.ISSUE_SUCCESS";
        public const string Paid = $"{Prefix}.PAYMENT_SUCCESS";
        public const string PaymentApplied = $"{Prefix}.ALLOCATION_SUCCESS";
        public const string Cancelled = $"{Prefix}.CANCEL_SUCCESS";
    }

    public static class Payments
    {
        private const string Prefix = "BILLING.PAYMENT";
        public const string Initiated = $"{Prefix}.INTENT_CREATED";
        public const string Completed = $"{Prefix}.PROCESS_SUCCESS";
        public const string Failed = $"{Prefix}.PROCESS_FAILED";
    }

    public static class Webhooks
    {
        private const string Prefix = "FINANCIAL.WEBHOOK";
        public const string Received = $"{Prefix}.RECEIVED";
        public const string Deduped = $"{Prefix}.DEDUPED";
        public const string Processed = $"{Prefix}.PROCESSED";
        public const string Failed = $"{Prefix}.FAILED";
    }
}
