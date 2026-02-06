using Cobryx.Application.Webhooks.Interfaces;
using System.Text.Json;

namespace Cobryx.Infrastructure.Webhooks.Stripe;

public class StripeWebhookParser : IWebhookParser
{
    public string Provider => "Stripe";

    public Task<WebhookParseResult> ParseAsync(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        
        var type = root.GetProperty("type").GetString();
        var data = root.GetProperty("data").GetProperty("object");

        var result = type switch
        {
            "payment_intent.succeeded" => new WebhookParseResult(
                "PaymentSucceeded",
                data,
                data.GetProperty("id").GetString()),
            
            "payment_intent.payment_failed" => new WebhookParseResult(
                "PaymentFailed",
                data,
                data.GetProperty("id").GetString()),

            _ => throw new NotSupportedException($"Stripe event type {type} is not supported.")
        };

        return Task.FromResult(result);
    }
}
