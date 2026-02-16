using System.ComponentModel.DataAnnotations;

namespace Cobryx.Infrastructure.Configuration;

public class CaptchaOptions
{
    public const string SectionName = "Security:Captcha";

    [Required]
    public string SecretKey { get; set; } = default!;

    public string SiteKey { get; set; } = string.Empty;
}
