namespace Cobryx.Api.Outcomes;

public static class PaymentOutcomes
{
    private const string Prefix = "BILLING.PAYMENT";

    public const string Registered = $"{Prefix}.REGISTER_SUCCESS";
    public const string Refunded = $"{Prefix}.REFUND_SUCCESS";
    public const string SearchCompleted = $"{Prefix}.SEARCH_SUCCESS";
}
