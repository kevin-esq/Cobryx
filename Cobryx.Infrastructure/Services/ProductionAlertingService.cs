using System.Text.Json;

using Cobryx.Application.Common.Interfaces;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Services;

public class ProductionAlertingService : IAlertingService
{
    private readonly ILogger<ProductionAlertingService> _logger;
    private readonly IEmailService _emailService;
    private readonly ISlackService _slackService;

    public ProductionAlertingService(
        ILogger<ProductionAlertingService> logger,
        IEmailService emailService,
        ISlackService slackService)
    {
        _logger = logger;
        _emailService = emailService;
        _slackService = slackService;
    }

    public async Task SendAlertAsync(
        string source,
        string message,
        AlertLevel level,
        object? metadata = null,
        CancellationToken ct = default)
    {
        var timestamp = DateTime.UtcNow;
        var metadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : "{}";

        // 1. Mandatory Logging (Production standard)
        var logMessage = $"[ALERT][{level}][{source}] {message} | Metadata: {metadataJson}";

        if (level == AlertLevel.Critical)
        {
            _logger.LogCritical(logMessage);
        }
        else if (level == AlertLevel.Degraded)
        {
            _logger.LogError(logMessage);
        }
        else
        {
            _logger.LogInformation(logMessage);
        }

        // 2. Dispatch to High-Priority Channels (Critical/Degraded only)
        if (level >= AlertLevel.Degraded)
        {
            await DispatchToCommunicationsAsync(source, message, level, metadataJson, ct);
        }
    }

    private async Task DispatchToCommunicationsAsync(
        string source,
        string message,
        AlertLevel level,
        string metadataJson,
        CancellationToken ct)
    {
        try
        {
            // 1. Dispatch to Slack (Modern standard)
            await _slackService.SendAlertAsync(message, $"Cobryx Alert: [{level}] from {source}", GetColor(level));

            // 2. Dispatch to Email (Reliable fallback)
            var subject = $"Cobryx Alert: [{level}] from {source}";
            var body = $"""
                <h3>Cobryx System Alert</h3>
                <p><b>Level:</b> {level}</p>
                <p><b>Source:</b> {source}</p>
                <p><b>Message:</b> {message}</p>
                <br/>
                <p><b>Metadata:</b></p>
                <pre>{metadataJson}</pre>
                <hr/>
                <p><small>This is an automated alert from the Cobryx Production Monitor.</small></p>
                """;

            await _emailService.SendEmailAsync("ops@cobryx.com", subject, body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch alert to external communications.");
        }
    }

    private static string GetColor(AlertLevel level) => level switch
    {
        AlertLevel.Critical => "#FF0000", // Red
        AlertLevel.Degraded => "#FFA500", // Orange
        _ => "#32CD32" // LimeGreen
    };
}
