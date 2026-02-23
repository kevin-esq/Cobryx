using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;

namespace Cobryx.Domain.Entities;

public class TenantInvitation : BaseEntity, IAggregateRoot
{
    public string Email { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid RoleId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public InvitationStatus Status { get; private set; }

    // Required for EF Core
    private TenantInvitation()
    {
        Email = null!;
        TokenHash = null!;
    }

    public TenantInvitation(string email, Guid tenantId, Guid roleId, string tokenHash, DateTime expiresAt)
    {
        Email = email.ToLowerInvariant();
        TenantId = tenantId;
        RoleId = roleId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        Status = InvitationStatus.Pending;
    }

    public void Accept()
    {
        if (!IsActive())
            throw new InvalidOperationException("This invitation is no longer active.");

        Status = InvitationStatus.Accepted;
    }

    public void Revoke()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException("Only pending invitations can be revoked.");

        Status = InvitationStatus.Revoked;
    }

    public bool IsActive()
    {
        return Status == InvitationStatus.Pending && DateTime.UtcNow < ExpiresAt;
    }

    public void MarkExpired()
    {
        if (Status == InvitationStatus.Pending && DateTime.UtcNow >= ExpiresAt)
        {
            Status = InvitationStatus.Expired;
        }
    }
}
