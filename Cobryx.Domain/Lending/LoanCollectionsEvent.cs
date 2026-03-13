using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;
namespace Cobryx.Domain.Lending;

/// <summary>
/// Audit trail for collections-related actions and state changes.
/// </summary>
public class LoanCollectionsEvent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid LoanId { get; private set; }
    public CollectionsEventType Type { get; private set; }
    public int DaysPastDue { get; private set; }
    public string? Description { get; private set; }
    public string? Metadata { get; private set; }

    public virtual Loan Loan { get; private set; } = null!;

    private LoanCollectionsEvent() { }

    public LoanCollectionsEvent(
        Guid tenantId,
        Guid loanId,
        CollectionsEventType type,
        int dpd,
        string? description = null,
        string? metadata = null)
    {
        TenantId = tenantId;
        LoanId = loanId;
        Type = type;
        DaysPastDue = dpd;
        Description = description;
        Metadata = metadata;
    }
}
