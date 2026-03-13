namespace Cobryx.Application.Common.Interfaces;

public interface IAuthAttemptService
{
    public Task<int> GetAttemptCountAsync(string ipAddress);
    public Task<int> GetUserAttemptCountAsync(string email);
    public Task IncrementAttemptsAsync(string ipAddress, string? email = null);
    public Task ResetAttemptsAsync(string ipAddress, string? email = null);
}
