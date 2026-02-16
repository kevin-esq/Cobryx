using System.ComponentModel.DataAnnotations;

namespace Cobryx.Infrastructure.Configuration;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string WebhookSecret { get; set; } = string.Empty;

    [Required, Url]
    public string SuccessUrl { get; set; } = string.Empty;

    [Required, Url]
    public string CancelUrl { get; set; } = string.Empty;
}
