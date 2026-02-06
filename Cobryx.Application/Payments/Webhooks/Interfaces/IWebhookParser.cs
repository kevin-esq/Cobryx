namespace Cobryx.Application.Webhooks.Interfaces;

public record WebhookParseResult(
    string InternalEventType,
    object Data, 
    string? ExternalTransactionId = null);

public interface IWebhookParser
{
    string Provider { get; }
    Task<WebhookParseResult> ParseAsync(string json);
}
