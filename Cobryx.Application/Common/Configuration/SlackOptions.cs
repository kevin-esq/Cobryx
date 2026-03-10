namespace Cobryx.Application.Common.Configuration;

public class SlackOptions
{
    public const string SectionName = "Slack";
    public string? WebhookUrl { get; set; }
    public string? Channel { get; set; }
}
