using Microsoft.AspNetCore.Http;
using Cobryx.Application.Common.Interfaces;

namespace Cobryx.Infrastructure.Services;

public class HttpContextService : IHttpContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return "0.0.0.0";

        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        }

        return ipAddress ?? "0.0.0.0";
    }

    public string GetUserAgent()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return "Unknown";

        var userAgent = context.Request.Headers["User-Agent"].ToString();
        return string.IsNullOrWhiteSpace(userAgent) ? "Unknown" : userAgent;
    }

    public string GetDeviceFingerprint()
    {
        var userAgent = GetUserAgent();
        var ipAddress = GetIpAddress();

        return $"{userAgent}_{ipAddress}".GetHashCode().ToString("X");
    }
}
