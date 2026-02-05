namespace Cobryx.Api.Outcomes;

public static class FinancialOutcomes
{
    public static class Taxes
    {
        private const string Prefix = "FINANCIAL.TAX";
        public const string SearchCompleted = $"{Prefix}.SEARCH.COMPLETED";
        public const string Created = $"{Prefix}.CREATED";
        public const string Deleted = $"{Prefix}.DELETED";
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
    }
}
