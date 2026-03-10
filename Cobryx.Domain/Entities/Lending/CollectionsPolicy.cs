using Cobryx.Domain.Common;
using Cobryx.Domain.Exceptions;

namespace Cobryx.Domain.Entities.Lending;

/// <summary>
/// Tenant-specific rules for delinquency stages and automated actions.
/// </summary>
public class CollectionsPolicy : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    // Thresholds
    public int EarlyStageDays { get; private set; } = 1;
    public int ModerateStageDays { get; private set; } = 31;
    public int SevereStageDays { get; private set; } = 61;
    public int DefaultStageDays { get; private set; } = 91;
    public int WriteOffDays { get; private set; } = 121;

    // Automated Toggles
    public bool EnableLateFees { get; private set; }
    public bool EnableAutoWriteOff { get; private set; }

    // Configuration
    public Guid? LateFeePolicyId { get; private set; }
    public virtual LateFeePolicy? LateFeePolicy { get; private set; }

    private CollectionsPolicy() { }

    public static CollectionsPolicy CreateStandard(Guid tenantId, string name)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);

        return new CollectionsPolicy
        {
            TenantId = tenantId,
            Name = name,
            EarlyStageDays = 1,
            ModerateStageDays = 31,
            SevereStageDays = 61,
            DefaultStageDays = 91,
            WriteOffDays = 121,
            EnableLateFees = true,
            EnableAutoWriteOff = false
        };
    }

    public void UpdateThresholds(int moderate, int severe, int @default, int writeOff)
    {
        ModerateStageDays = moderate;
        SevereStageDays = severe;
        DefaultStageDays = @default;
        WriteOffDays = writeOff;
        UpdateTimestamp();
    }
}
