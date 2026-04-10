namespace Cobryx.Api.Controllers.V1;

public partial class WebhooksController
{
    [LoggerMessage(1, LogLevel.Warning,
        "Stripe webhook received without signature verification (WebhookSecret missing)")]
    private static partial void LogWebhookMissingSecret(ILogger logger);

    [LoggerMessage(2, LogLevel.Warning,
        "Stripe webhook signature verification failed")]
    private static partial void LogSignatureVerificationFailed(ILogger logger, Exception? ex);

    [LoggerMessage(3, LogLevel.Warning,
        "Webhook ingestion failed for event {StripeEventId}: {Error}")]
    private static partial void LogIngestionFailed(ILogger logger, string stripeEventId, string error);

    [LoggerMessage(4, LogLevel.Warning,
        "Webhook processing failed for event {StripeEventId}: {Error}")]
    private static partial void LogProcessingFailed(ILogger logger, string stripeEventId, string error);

    [LoggerMessage(5, LogLevel.Error,
        "Unhandled error processing Stripe webhook {StripeEventId}")]
    private static partial void LogProcessingError(ILogger logger, Exception? ex, string stripeEventId);
}
