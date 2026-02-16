namespace Cobryx.Api.Outcomes;

public static class WebhookOutcomes
{
    private const string Prefix = "API.WEBHOOK";

    public const string Received = $"{Prefix}.RECEIVED";
    public const string EmptyBody = $"{Prefix}.EMPTY_BODY";
    public const string InvalidSignature = $"{Prefix}.INVALID_SIGNATURE";
}
