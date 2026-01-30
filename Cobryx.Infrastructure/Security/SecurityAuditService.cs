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
        // CEF:Version|Device Vendor|Device Product|Device Version|Device Event Class ID|Name|Severity|[Extension]
        var cef = $"CEF:0|Cobryx|Cobryx.Api|1.0|AUTH_SUCCESS|{eventName}|1|src={ipAddress} suser={userId ?? "Anonymous"} msg=Authentication successful";
        _logger.LogInformation(cef);
    }

    public void LogFailure(string eventName, string? userId, string ipAddress, string reason, object? metadata = null)
    {
        var cef = $"CEF:0|Cobryx|Cobryx.Api|1.0|AUTH_FAILURE|{eventName}|5|src={ipAddress} suser={userId ?? "Anonymous"} msg={reason}";
        _logger.LogWarning(cef);
    }

    public void LogCritical(string eventName, string? userId, string ipAddress, string reason, object? metadata = null)
    {
        var cef = $"CEF:0|Cobryx|Cobryx.Api|1.0|SECURITY_CRITICAL|{eventName}|10|src={ipAddress} suser={userId ?? "Anonymous"} msg={reason}";
        _logger.LogCritical(cef);
    }

    public void LogSecurityAlert(string eventName, string? userId, string ipAddress, string description, object? metadata = null)
    {
        var cef = $"CEF:0|Cobryx|Cobryx.Api|1.0|SECURITY_ALERT|{eventName}|8|src={ipAddress} suser={userId ?? "Anonymous"} msg={description}";
        _logger.LogError(cef);
    }
}
