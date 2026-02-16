using System.ComponentModel.DataAnnotations;

namespace Cobryx.Infrastructure.Configuration;

public class Fido2Options
{
    public const string SectionName = "Fido2";

    [Required]
    public string Origin { get; set; } = default!;

    [Required]
    public string ServerDomain { get; set; } = default!;
}
