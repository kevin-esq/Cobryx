using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

public static class NotificationOutcomes
{
    private const string Prefix = "NOTIFICATION";

    public static readonly Outcome SearchCompleted = new($"{Prefix}.SEARCH_SUCCESS",
        OutcomeCategory.Success, "Notifications retrieved successfully.");

    public static readonly Outcome MarkedAsRead = new($"{Prefix}.MARKED_READ",
        OutcomeCategory.Success, "Notification marked as read.");

    public static readonly Outcome AllMarkedAsRead = new($"{Prefix}.ALL_MARKED_READ",
        OutcomeCategory.Success, "All notifications marked as read.");
}
