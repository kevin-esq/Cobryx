using System.ComponentModel.DataAnnotations;

namespace Cobryx.Infrastructure.Configuration;

public class JwtOptions
{
    public const string SectionName = "JwtSettings";

    [Required]
    public string Secret { get; set; } = default!;

    [Required]
    public string Issuer { get; set; } = default!;

    [Required]
    public string Audience { get; set; } = default!;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 60;
}
