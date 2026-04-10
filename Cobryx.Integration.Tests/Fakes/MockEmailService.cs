using Cobryx.Application.Common.Interfaces;

namespace Cobryx.Integration.Tests.Fakes;

public class MockEmailService : IEmailService
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _sentEmails = new();

    public string? LastTo { get; private set; }
    public string? LastSubject { get; private set; }
    public string? LastBody { get; private set; }
    public string? LastToken => ExtractToken(LastBody);

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        LastTo = to;
        LastSubject = subject;
        LastBody = body;
        _sentEmails[to] = body;
        return Task.CompletedTask;
    }

    public string? GetLastToken(string email)
    {
        if (_sentEmails.TryGetValue(email, out var body))
        {
            return ExtractToken(body);
        }
        return null;
    }

    private static string? ExtractToken(string? body)
    {
        if (string.IsNullOrEmpty(body))
            return null;
        var parts = body.Split("token=");
        return parts.Length > 1 ? parts[1].Split("&")[0] : null;
    }
}
