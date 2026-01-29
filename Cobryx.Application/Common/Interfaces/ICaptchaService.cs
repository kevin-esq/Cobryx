namespace Cobryx.Application.Common.Interfaces;

public interface ICaptchaService
{
    Task<bool> VerifyAsync(string? token, string? ipAddress = null, CancellationToken cancellationToken = default);
}
