using System.Net;
using System.Net.Mail;
using Cobryx.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Cobryx.Infrastructure.Configuration;

namespace Cobryx.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = _settings.SmtpHost;
        var port = _settings.SmtpPort;
        var username = _settings.SmtpUsername;
        var password = _settings.SmtpPassword;
        var from = _settings.FromAddress;

        if (string.IsNullOrEmpty(host) || host == "localhost")
        {
            _logger.LogInformation("Simulating email to {To} with subject {Subject}", to, subject);
            _logger.LogDebug("Email Body: {Body}", body);
            return;
        }

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password)
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mailMessage.To.Add(to);

        await client.SendMailAsync(mailMessage, cancellationToken);
        _logger.LogInformation("Email sent successfully to {To}", to);
    }
}
