using Cobryx.Application.Common.Interfaces;

namespace Cobryx.Integration.Tests.Fakes;

public class MockCaptchaService : ICaptchaService
{
    public Task<bool> VerifyAsync(string? token, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
