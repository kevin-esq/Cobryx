using Cobryx.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Cobryx.Infrastructure.Services;

public class ProductionAlertingService : IAlertingService
{
    private readonly ILogger<ProductionAlertingService> _logger;
    private readonly IEmailService _emailService;

    public ProductionAlertingService(ILogger<ProductionAlertingService> logger, IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
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
            // For now, we use Email as the reliable fallback.
            // In a real env, this is where we'd plug in Slack/Discord/PagerDuty.
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

            // TARGET: Ops Team
            await _emailService.SendEmailAsync("ops@cobryx.com", subject, body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch alert to external communications.");
        }
    }
}
