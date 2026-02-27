using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;

namespace Cobryx.Domain.Entities.Accounting;

/// <summary>
/// Represents a Ledger Account in a multi-tenant double-entry system.
/// </summary>
public class LedgerAccount : BaseEntity
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty; // e.g., "1010"
    public string Name { get; private set; } = string.Empty; // e.g., "Cash at Bank"
    public LedgerAccountType Type { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; } // Prevents deletion of core accounts

    private LedgerAccount() { } // EF Core

    public LedgerAccount(
        Guid tenantId,
        string code,
        string name,
        LedgerAccountType type,
        string currency = "MXN",
        bool isSystem = false)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        Type = type;
        Currency = currency.ToUpperInvariant();
        IsSystem = isSystem;
    }

    public void UpdateDetails(string name, string code)
    {
        if (IsSystem)
            throw new DomainException(DomainErrorCode.Common.GeneralError); // Or specific "SystemAccountLocked"

        Name = name;
        Code = code;
        UpdateTimestamp();
    }
}
