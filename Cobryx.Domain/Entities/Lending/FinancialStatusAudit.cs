using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Lending.Enums;

namespace Cobryx.Domain.Entities.Lending;

/// <summary>
/// Immutable audit record of a loan's financial risk level transition.
/// Essential for regulatory compliance and advanced risk modeling.
/// </summary>
public class FinancialStatusAudit : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid LoanId { get; private set; }
    public FinancialStatus OldStatus { get; private set; }
    public FinancialStatus NewStatus { get; private set; }
    public int DaysPastDue { get; private set; }
    public decimal ArrearsAmount { get; private set; }
    public string? Reason { get; private set; }

    private FinancialStatusAudit() { }

    public FinancialStatusAudit(
        Guid tenantId,
        Guid loanId,
        FinancialStatus oldStatus,
        FinancialStatus newStatus,
        int dpd,
        decimal arrears,
        string? reason = null)
    {
        TenantId = tenantId;
        LoanId = loanId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        DaysPastDue = dpd;
        ArrearsAmount = arrears;
        Reason = reason;
    }
}
