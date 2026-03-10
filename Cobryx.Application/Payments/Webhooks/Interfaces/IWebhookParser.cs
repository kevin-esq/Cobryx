namespace Cobryx.Application.Payments.Webhooks.Interfaces;

public record WebhookParseResult(
    string InternalEventType,
    object Data,
    string? ExternalTransactionId = null,
    Dictionary<string, string>? Metadata = null);

public interface IWebhookParser
{
    string Provider { get; }
    Task<WebhookParseResult> ParseAsync(string json, string? signature = null);
}
