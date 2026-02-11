namespace Cobryx.Application.Common.Interfaces;

public interface IAuthAttemptService
{
    Task<int> GetAttemptCountAsync(string ipAddress);
    Task<int> GetUserAttemptCountAsync(string email);
    Task IncrementAttemptsAsync(string ipAddress, string? email = null);
    Task ResetAttemptsAsync(string ipAddress, string? email = null);
}
