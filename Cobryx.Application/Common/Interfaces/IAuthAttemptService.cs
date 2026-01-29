namespace Cobryx.Application.Common.Interfaces;

public interface IAuthAttemptService
{
    Task<int> GetAttemptCountAsync(string ipAddress);
    Task IncrementAttemptsAsync(string ipAddress);
    Task ResetAttemptsAsync(string ipAddress);
}
