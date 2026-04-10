using System.ComponentModel.DataAnnotations;

namespace Cobryx.Infrastructure.Configuration;

public class GoogleOAuthOptions
{
    public const string SectionName = "OAuth:Google";

    [Required]
    public string ClientId { get; set; } = default!;
}
