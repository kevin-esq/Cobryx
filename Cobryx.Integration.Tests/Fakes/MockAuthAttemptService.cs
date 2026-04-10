using Cobryx.Application.Common.Interfaces;

namespace Cobryx.Integration.Tests.Fakes;

public class MockAuthAttemptService : IAuthAttemptService
{
    public Task<int> GetAttemptCountAsync(string ipAddress) => Task.FromResult(0);
    public Task<int> GetUserAttemptCountAsync(string email) => Task.FromResult(0);
    public Task IncrementAttemptsAsync(string ipAddress, string? email = null) => Task.CompletedTask;
    public Task ResetAttemptsAsync(string ipAddress, string? email = null) => Task.CompletedTask;
}
