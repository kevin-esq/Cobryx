using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid UserId { get; private set; }
    public string EntityName { get; private set; }
    public string EntityId { get; private set; }
    public string Action { get; private set; } // "Create", "Update", "Delete"
    public string? OldValues { get; private set; } // JSON Snapshot
    public string? NewValues { get; private set; } // JSON Snapshot

    private AuditLog() { }

    public AuditLog(Guid userId, string entityName, string entityId, string action, string? oldValues, string? newValues)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required for audit.");
        if (string.IsNullOrWhiteSpace(entityName)) throw new ArgumentException("EntityName is required.");
        
        UserId = userId;
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        OldValues = oldValues;
        NewValues = newValues;
    }
}
