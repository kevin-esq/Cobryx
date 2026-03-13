namespace Cobryx.Application.Common.Interfaces;

public interface IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
