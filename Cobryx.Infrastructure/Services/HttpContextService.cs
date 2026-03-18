using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Microsoft.AspNetCore.Http;

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
        if (context == null)
            return CobryxDefaults.FallbackIpAddress;

        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        }

        return ipAddress ?? CobryxDefaults.FallbackIpAddress;
    }

    public string GetUserAgent()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return CobryxDefaults.UnknownValue;

        var userAgent = context.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(userAgent) ? CobryxDefaults.UnknownValue : userAgent;
    }

    public string GetDeviceFingerprint()
    {
        var userAgent = GetUserAgent();
        var ipAddress = GetIpAddress();

        return $"{userAgent}_{ipAddress}".GetHashCode().ToString("X");
    }
}
