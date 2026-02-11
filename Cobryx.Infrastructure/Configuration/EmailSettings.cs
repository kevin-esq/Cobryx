namespace Cobryx.Infrastructure.Configuration;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 25;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string FromAddress { get; set; } = "[EMAIL_ADDRESS]";
}
