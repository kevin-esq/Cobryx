using System;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class User : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Role { get; private set; } // "Owner", "Admin", "Collector"

    public User(Guid tenantId, string fullName, string email, string passwordHash, string role)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("FullName is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        
        TenantId = tenantId;
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }
}
