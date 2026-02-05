using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class AuditLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string EntityName { get; private set; }
    public string EntityId { get; private set; }
    public string Action { get; private set; }
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private AuditLog()
    {
        EntityName = null!;
        EntityId = null!;
        Action = null!;
    }

    public AuditLog(
        Guid tenantId,
        Guid? userId,
        string entityName,
        string entityId,
        string action,
        string? oldValues = null,
        string? newValues = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (tenantId == Guid.Empty) throw new DomainException("DOMAIN.TENANT_ID_REQUIRED");
        if (string.IsNullOrWhiteSpace(entityName)) throw new DomainException("DOMAIN.ENTITY_NAME_REQUIRED");

        TenantId = tenantId;
        UserId = userId;
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        OldValues = oldValues;
        NewValues = newValues;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
