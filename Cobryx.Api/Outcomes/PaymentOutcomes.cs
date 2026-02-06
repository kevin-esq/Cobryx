namespace Cobryx.Api.Outcomes;

public static class PaymentOutcomes
{
    private const string Prefix = "PAYMENT";

    public const string Registered = $"{Prefix}.REGISTERED";
    public const string Refunded = $"{Prefix}.REFUNDED";
    public const string SearchCompleted = $"{Prefix}.SEARCH.COMPLETED";
}
