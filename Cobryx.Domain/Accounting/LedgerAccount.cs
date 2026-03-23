using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Shared;


namespace Cobryx.Domain.Accounting;

/// <summary>
/// Represents a Ledger Account in a multi-tenant double-entry system.
/// </summary>
public class LedgerAccount : BaseEntity
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public LedgerAccountType Type { get; private set; }
    public LedgerAccountRole Role { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public bool IsSystem { get; private set; }

    private LedgerAccount() { }

    public LedgerAccount(
        Guid tenantId,
        string code,
        string name,
        LedgerAccountType type,
        LedgerAccountRole role = LedgerAccountRole.None,
        string currency = "MXN",
        bool isSystem = false)
    {
        TenantId = tenantId;
        Code = code;
        Name = name;
        Type = type;
        Role = role;
        Currency = currency.ToUpperInvariant();
        IsSystem = isSystem;
    }

    public void UpdateDetails(string name, string code)
    {
        if (IsSystem)
            throw new DomainException(DomainErrorCode.Common.GeneralError);

        Name = name;
        Code = code;
        UpdateTimestamp();
    }
}
