using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Identity;

public enum NotificationType
{
    Information,
    Success,
    Warning,
    Error
}

public class Notification : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; }
    public string? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }

    private Notification() { }

    public Notification(
        Guid tenantId,
        string title,
        string message,
        NotificationType type = NotificationType.Information,
        Guid? userId = null,
        string? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        TenantId = tenantId;
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        IsRead = false;
        RelatedEntityId = relatedEntityId;
        RelatedEntityType = relatedEntityType;
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            UpdateTimestamp();
        }
    }
}
