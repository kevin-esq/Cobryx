namespace Cobryx.Application.Common.Interfaces;

public interface ISecurityAuditService
{
    void LogSuccess(string eventName, string? userId, string ipAddress, object? metadata = null);
    void LogFailure(string eventName, string? userId, string ipAddress, string reason, object? metadata = null);
    void LogCritical(string eventName, string? userId, string ipAddress, string reason, object? metadata = null);
    void LogSecurityAlert(string eventName, string? userId, string ipAddress, string description, object? metadata = null);
}
