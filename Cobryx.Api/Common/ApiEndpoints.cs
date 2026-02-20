namespace Cobryx.Api.Common;

/// <summary>
/// Centralized API endpoint segment constants for middleware filtering and routing logic.
/// </summary>
public static class ApiEndpoints
{
    public const string Health = "/health";
    public const string Auth = "/auth";
    public const string Webhooks = "/webhooks";
    public const string Swagger = "/swagger";
    public const string Hangfire = "/hangfire";
    public const string Metrics = "/metrics";
    public const string Ping = "/ping";
    public const string Root = "/";
}
