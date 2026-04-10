namespace Cobryx.Application.Notifications.Common;

/// <summary>
/// Notification projection returned by the API.
/// Maps 1:1 with the public contract to preserve response structure.
/// </summary>
public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string? Category,
    bool IsRead,
    DateTime CreatedAt);
