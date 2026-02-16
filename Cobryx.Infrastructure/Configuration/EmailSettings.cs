using System.ComponentModel.DataAnnotations;

namespace Cobryx.Infrastructure.Configuration;

public class EmailSettings
{
    [Required]
    public string SmtpHost { get; set; } = "localhost";

    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 25;

    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }

    [Required, EmailAddress]
    public string FromAddress { get; set; } = "[EMAIL_ADDRESS]";
}
