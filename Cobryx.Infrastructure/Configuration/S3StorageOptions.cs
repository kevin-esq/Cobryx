using System.ComponentModel.DataAnnotations;

namespace Cobryx.Infrastructure.Configuration;

public class S3StorageOptions
{
    public const string SectionName = "Storage:S3";

    [Required]
    public string AccessKey { get; set; } = default!;

    [Required]
    public string SecretKey { get; set; } = default!;

    [Required]
    [Url]
    public string ServiceUrl { get; set; } = default!;

    [Required]
    public string BucketName { get; set; } = default!;
}
