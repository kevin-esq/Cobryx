using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

/// <summary>
/// Mandatory audit record for administrative actions and manual overrides.
/// Ensures transparency and accountability for high-privileged operations.
/// </summary>
public class AdminActionAudit : BaseEntity
{
    public Guid AdminUserId { get; private set; }
    public string ActionName { get; private set; } = null!;
    public string? TargetType { get; private set; }
    public Guid? TargetId { get; private set; }
    public string? Reason { get; private set; }

    private AdminActionAudit() { }

    public AdminActionAudit(
        Guid adminUserId,
        string actionName,
        string? targetType = null,
        Guid? targetId = null,
        string? reason = null,
        string? metadataJson = null)
    {
        if (adminUserId == Guid.Empty) throw new ArgumentException("AdminUserId is required", nameof(adminUserId));
        if (string.IsNullOrWhiteSpace(actionName)) throw new ArgumentException("ActionName is required", nameof(actionName));

        AdminUserId = adminUserId;
        ActionName = actionName;
        TargetType = targetType;
        TargetId = targetId;
        Reason = reason;
        MetadataJson = metadataJson;
    }
}
