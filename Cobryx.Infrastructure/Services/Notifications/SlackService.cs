using System.Text;
using System.Text.Json;

using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Infrastructure.Services.Notifications;

public class SlackService : ISlackService
{
    private readonly HttpClient _httpClient;
    private readonly SlackOptions _options;
    private readonly ILogger<SlackService> _logger;

    public SlackService(HttpClient httpClient, IOptions<SlackOptions> options, ILogger<SlackService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAlertAsync(string message, string? title = null, string? color = null)
    {
        if (string.IsNullOrEmpty(_options.WebhookUrl))
        {
            _logger.LogWarning("Slack WebhookUrl is not configured. Alert suppressed.");
            return;
        }

        var payload = new
        {
            attachments = new[]
            {
                new
                {
                    title = title ?? "System Alert",
                    text = message,
                    color = color ?? "#ff0000",
                    ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            }
        };

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_options.WebhookUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send Slack alert. Status: {Status}, Error: {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending Slack alert.");
        }
    }
}
