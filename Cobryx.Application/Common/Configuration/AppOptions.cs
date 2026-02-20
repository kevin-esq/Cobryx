using System.ComponentModel.DataAnnotations;

namespace Cobryx.Application.Common.Configuration;

public class AppOptions
{
    [Required, Url]
    public string BaseUrl { get; set; } = "https://cobryx.com.mx";

    [Required, Url]
    public string AppUrl { get; set; } = "https://app.cobryx.com.mx";

    [Required, Url]
    public string DocsUrl { get; set; } = "https://docs.api.cobryx.com.mx";

    [Required]
    public string InvitationTokenSecret { get; set; } = string.Empty;

    public string? OldInvitationTokenSecret { get; set; }

    public int InvitationTokenTTLHours { get; set; } = 72;

    public int MaxInvitationCleanupBatchSize { get; set; } = 500;

    public int MinEnrollmentResponseTimeMs { get; set; } = 0;
}
