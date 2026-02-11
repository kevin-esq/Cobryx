namespace Cobryx.Api.Outcomes;

public static class InvoicingOutcomes
{
    public static class Taxes
    {
        private const string Prefix = "FINANCIAL.TAX";
        public const string SearchCompleted = $"{Prefix}.SEARCH.COMPLETED";
        public const string Created = $"{Prefix}.CREATED";
        public const string Deleted = $"{Prefix}.DELETED";

        public const string ValidationFailed = $"{Prefix}.VALIDATION_FAILED";
        public const string Conflict = $"{Prefix}.CONFLICT";
    }

    public static class PaymentMethods
    {
        private const string Prefix = "FINANCIAL.PAYMENT_METHOD";
        public const string SearchCompleted = $"{Prefix}.SEARCH.COMPLETED";
        public const string Created = $"{Prefix}.CREATED";
        public const string Deleted = $"{Prefix}.DELETED";
    }

    public static class Invoices
    {
        private const string Prefix = "FINANCIAL.INVOICE";
        public const string SearchCompleted = $"{Prefix}.SEARCH.COMPLETED";
        public const string Created = $"{Prefix}.CREATED";
        public const string Issued = $"{Prefix}.ISSUED";
        public const string Paid = $"{Prefix}.PAID";
        public const string PaymentApplied = $"{Prefix}.PAYMENT_APPLIED";
        public const string Cancelled = $"{Prefix}.CANCELLED";
    }

    public static class Payments
    {
        private const string Prefix = "FINANCIAL.PAYMENT";
        public const string Initiated = $"{Prefix}.INITIATED";
        public const string Completed = $"{Prefix}.COMPLETED";
        public const string Failed = $"{Prefix}.FAILED";
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
