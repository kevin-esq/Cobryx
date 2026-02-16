using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public enum SupportTicketStatus { Open, InProgress, Resolved, Closed }
public enum SupportTicketPriority { Low, Medium, High, Critical }
public enum SupportTicketCategory { Inquiry, Bug, FeatureRequest, TechnicalSupport, Billing }

public class SupportTicket : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public SupportTicketStatus Status { get; private set; }
    public SupportTicketPriority Priority { get; private set; }
    public SupportTicketCategory Category { get; private set; }

    private SupportTicket()
    {
        Title = null!;
        Description = null!;
    }

    public SupportTicket(
        Guid tenantId,
        Guid userId,
        string title,
        string description,
        SupportTicketPriority priority = SupportTicketPriority.Medium,
        SupportTicketCategory category = SupportTicketCategory.Inquiry)
    {
        TenantId = tenantId;
        UserId = userId;
        Title = title;
        Description = description;
        Status = SupportTicketStatus.Open;
        Priority = priority;
        Category = category;
    }

    public void UpdateStatus(SupportTicketStatus status, string? notes = null)
    {
        Status = status;
        if (notes != null)
        {
            var updatedNotes = string.IsNullOrEmpty(InternalNotes)
                ? $"[{DateTime.UtcNow}] {notes}"
                : $"{InternalNotes}\n[{DateTime.UtcNow}] {notes}";
            SetInternalNotes(updatedNotes);
        }
        UpdateTimestamp();
    }
}
