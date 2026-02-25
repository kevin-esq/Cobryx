namespace Cobryx.Application.Common.Interfaces;

public enum AlertLevel
{
    Info,
    Degraded,
    Critical
}

public interface IAlertingService
{
    Task SendAlertAsync(
        string source,
        string message,
        AlertLevel level,
        object? metadata = null,
        CancellationToken ct = default);
}
