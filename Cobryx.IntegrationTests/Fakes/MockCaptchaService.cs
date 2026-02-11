using Cobryx.Application.Common.Interfaces;

namespace Cobryx.IntegrationTests.Fakes;

public class MockCaptchaService : ICaptchaService
{
    public Task<bool> VerifyAsync(string? token, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
