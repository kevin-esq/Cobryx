using Cobryx.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Cobryx.Infrastructure.Security;

public class SecurityAuditService : ISecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(ILogger<SecurityAuditService> logger)
    {
        _logger = logger;
    }

    public void LogSuccess(string eventName, string? userId, string ipAddress, object? metadata = null)
    {
        _logger.LogInformation("SECURITY_SUCCESS | Event: {EventName} | User: {UserId} | IP: {IpAddress} | Metadata: {@Metadata}",
            eventName, userId ?? "Anonymous", ipAddress, metadata);
    }

    public void LogFailure(string eventName, string? userId, string ipAddress, string reason, object? metadata = null)
    {
        _logger.LogWarning("SECURITY_FAILURE | Event: {EventName} | User: {UserId} | IP: {IpAddress} | Reason: {Reason} | Metadata: {@Metadata}",
            eventName, userId ?? "Anonymous", ipAddress, reason, metadata);
    }

    public void LogCritical(string eventName, string? userId, string ipAddress, string reason, object? metadata = null)
    {
        _logger.LogCritical("SECURITY_CRITICAL | Event: {EventName} | User: {UserId} | IP: {IpAddress} | Reason: {Reason} | Metadata: {@Metadata}",
            eventName, userId ?? "Anonymous", ipAddress, reason, metadata);
    }

    public void LogSecurityAlert(string eventName, string? userId, string ipAddress, string description, object? metadata = null)
    {
        _logger.LogError("SECURITY_ALERT | Event: {EventName} | User: {UserId} | IP: {IpAddress} | Description: {Description} | Metadata: {@Metadata}",
            eventName, userId ?? "Anonymous", ipAddress, description, metadata);
    }
}
