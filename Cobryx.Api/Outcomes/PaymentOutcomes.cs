using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

public static class PaymentOutcomes
{
    private const string Prefix = "BILLING.PAYMENT";

    public static readonly Outcome Created = new($"{Prefix}.CREATED_SUCCESS", OutcomeCategory.Success, "Payment created successfully.");
    public static readonly Outcome Processed = new($"{Prefix}.PROCESSED_SUCCESS", OutcomeCategory.Success, "Payment processed successfully.");
    public static readonly Outcome SearchCompleted = new($"{Prefix}.SEARCH_SUCCESS", OutcomeCategory.Success, "Payment search completed.");
}
