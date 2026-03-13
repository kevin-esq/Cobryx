namespace Cobryx.Application.Common.Interfaces;

public interface ISecurityAuditService
{
    public void LogSuccess(string eventName, string? userId, string ipAddress, object? metadata = null);
    public void LogFailure(string eventName, string? userId, string ipAddress, string reason, object? metadata = null);
    public void LogCritical(string eventName, string? userId, string ipAddress, string reason, object? metadata = null);
    public void LogSecurityAlert(string eventName, string? userId, string ipAddress, string description, object? metadata = null);
}
