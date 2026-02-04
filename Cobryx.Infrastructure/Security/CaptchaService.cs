using System.Net.Http.Json;
using Cobryx.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Security;

public class TurnstileCaptchaService : ICaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;
    private readonly ILogger<TurnstileCaptchaService> _logger;

    public TurnstileCaptchaService(HttpClient httpClient, IConfiguration configuration, ILogger<TurnstileCaptchaService> logger)
    {
        _httpClient = httpClient;
        _secretKey = configuration["Security:Captcha:SecretKey"] ?? "1x0000000000000000000000000000000AA";
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string? token, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Turnstile verification failed: Token is empty.");
            return false;
        }

        try
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _secretKey),
                new KeyValuePair<string, string>("response", token),
                new KeyValuePair<string, string>("remoteip", ipAddress ?? "")
            });

            var response = await _httpClient.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content, cancellationToken);

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>(cancellationToken: cancellationToken);

            if (result == null || !result.Success)
            {
                _logger.LogWarning("Turnstile verification failed. Errors: {Errors}",
                    result?.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "None");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Turnstile verification.");
            return false;
        }
    }

    private class TurnstileResponse
    {
        public bool Success { get; set; }
        public string[]? ErrorCodes { get; set; }
    }
}
