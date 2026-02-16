using System.ComponentModel.DataAnnotations;

namespace Cobryx.Infrastructure.Configuration;

public class ClamAvOptions
{
    public const string SectionName = "Security:ClamAV";

    [Required]
    public string Host { get; set; } = default!;

    [Range(1, 65535)]
    public int Port { get; set; } = 3310;
}
