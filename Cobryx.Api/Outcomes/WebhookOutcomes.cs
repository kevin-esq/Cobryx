using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

public static class WebhookOutcomes
{
    private const string Prefix = "WEBHOOK";

    public static readonly Outcome Received = new($"{Prefix}.RECEIVED_SUCCESS", OutcomeCategory.Success, "Webhook received successfully.");
    public static readonly Outcome Processed = new($"{Prefix}.PROCESSED_SUCCESS", OutcomeCategory.Success, "Webhook processed successfully.");
    public static readonly Outcome SearchCompleted = new($"{Prefix}.SEARCH_SUCCESS", OutcomeCategory.Success, "Webhook search completed.");
    public static readonly Outcome InvalidSignature = new($"{Prefix}.INVALID_SIGNATURE", OutcomeCategory.BusinessError, "Webhook invalid signature.");
    public static readonly Outcome EmptyBody = new($"{Prefix}.EMPTY_BODY", OutcomeCategory.BusinessError, "Webhook empty body.");
    public static readonly Outcome ProcessingFailed = new($"{Prefix}.PROCESSING_FAILED", OutcomeCategory.BusinessError, "Webhook processing failed.");
}
