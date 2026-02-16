using System.ComponentModel.DataAnnotations;

namespace Cobryx.Infrastructure.Configuration;

public class CachingOptions
{
    public const string SectionName = "Caching";

    [Required]
    public RedisOptions Redis { get; set; } = default!;

    [Range(1, 86400)]
    public int DefaultTTL { get; set; } = 300;
}

public class RedisOptions
{
    [Required]
    public string ConnectionString { get; set; } = default!;
}
