namespace Cobryx.Application.Common.Interfaces;

public interface ICaptchaService
{
    public Task<bool> VerifyAsync(string? token, string? ipAddress = null, CancellationToken cancellationToken = default);
}
