using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Lending.Enums;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Exceptions;

namespace Cobryx.Domain.Entities.Lending;

/// <summary>
/// Mutable execution of a loan representing the living state of payments.
/// </summary>
public class Loan : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid LoanAgreementId { get; private set; }
    public string LoanNumber { get; private set; } = string.Empty;
    public LoanStatus Status { get; private set; }
    public LegalStatus LegalStatus { get; private set; }
    public RiskStatus RiskStatus { get; private set; }
    public CollectionStage CollectionStage { get; private set; }
    public decimal CurrentPrincipalBalance { get; private set; }
    public decimal CurrentInterestBalance { get; private set; }
    public decimal CurrentLateFeeBalance { get; private set; }
    public decimal TotalPaid { get; private set; }
    public DateTime? LastPaymentDate { get; private set; }
    public int DaysInArrears { get; private set; }
    public DateTime? NextPaymentDueDate { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public virtual LoanAgreement Agreement { get; private set; } = null!;

    private readonly List<Installment> _installments = new();
    public IReadOnlyCollection<Installment> Installments => _installments.AsReadOnly();

    private readonly List<Payment> _payments = new();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    private Loan() { }

    public Loan(
        Guid tenantId,
        Guid loanAgreementId,
        string loanNumber,
        decimal principalAmount)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);
        if (loanAgreementId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Loans.AgreementNotFound);
        if (string.IsNullOrWhiteSpace(loanNumber))
            throw new DomainException(DomainErrorCode.Common.EntityNameRequired);

        TenantId = tenantId;
        LoanAgreementId = loanAgreementId;
        LoanNumber = loanNumber;
        CurrentPrincipalBalance = principalAmount;
        CurrentInterestBalance = 0;
        CurrentLateFeeBalance = 0;
        TotalPaid = 0;
        DaysInArrears = 0;

        Status = LoanStatus.Pending;
        LegalStatus = LegalStatus.Active;
        RiskStatus = RiskStatus.OnTime;
        CollectionStage = CollectionStage.None;
    }

    public void Activate()
    {
        if (Status != LoanStatus.Pending)
            return;

        Status = LoanStatus.Active;
        UpdateTimestamp();
    }

    public void AddInstallments(IEnumerable<Installment> installments)
    {
        if (_installments.Any())
            throw new DomainException(DomainErrorCode.Credits.InstallmentsAlreadyGenerated);

        _installments.AddRange(installments);

        var firstPending = _installments
            .Where(i => i.Status == InstallmentStatus.Pending)
            .OrderBy(i => i.DueDate)
            .FirstOrDefault();

        NextPaymentDueDate = firstPending?.DueDate;
    }

    public void RecordPaymentApplied(decimal amount, DateTime paymentDate)
    {
        if (amount <= 0)
            throw new DomainException(DomainErrorCode.Loans.InvalidPaymentAmount);

        if (Status == LoanStatus.Closed)
            throw new DomainException(DomainErrorCode.Loans.AlreadyClosed);

        TotalPaid += amount;
        LastPaymentDate = paymentDate;
        UpdateTimestamp();
    }

    public void RecalculateBalances()
    {
        CurrentPrincipalBalance = _installments
            .Sum(i => i.PrincipalAmount - i.PrincipalPaid);

        CurrentInterestBalance = _installments
            .Sum(i => i.InterestAmount - i.InterestPaid);

        var nextPending = _installments
            .Where(i => i.Status != InstallmentStatus.Paid)
            .OrderBy(i => i.DueDate)
            .FirstOrDefault();

        NextPaymentDueDate = nextPending?.DueDate;

        if (_installments.All(i => i.Status == InstallmentStatus.Paid))
        {
            MarkAsClosed();
        }

        UpdateTimestamp();
    }

    public void UpdateRiskStatus()
    {
        var today = DateTime.UtcNow.Date;

        var oldestOverdue = _installments
            .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate < today)
            .OrderBy(i => i.DueDate)
            .FirstOrDefault();

        if (oldestOverdue == null)
        {
            DaysInArrears = 0;
            RiskStatus = RiskStatus.OnTime;
            CollectionStage = CollectionStage.None;
        }
        else
        {
            DaysInArrears = (int)(today - oldestOverdue.DueDate).TotalDays;

            RiskStatus = DaysInArrears switch
            {
                <= 0 => RiskStatus.OnTime,
                <= 30 => RiskStatus.Late,
                <= 60 => RiskStatus.SevereLate,
                _ => RiskStatus.Critical
            };

            CollectionStage = RiskStatus switch
            {
                RiskStatus.OnTime => CollectionStage.None,
                RiskStatus.Late => CollectionStage.Friendly,
                RiskStatus.SevereLate => CollectionStage.Hard,
                RiskStatus.Critical => CollectionStage.Legal,
                _ => CollectionStage.None
            };
        }

        UpdateTimestamp();
    }

    public void MarkAsClosed()
    {
        if (Status == LoanStatus.Closed)
            return;

        Status = LoanStatus.Closed;
        LegalStatus = LegalStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void MarkAsDefaulted()
    {
        Status = LoanStatus.Defaulted;
        LegalStatus = LegalStatus.Defaulted;
        UpdateTimestamp();
    }

    public void AccrueInterest(DateTime asOfDate, decimal dailyRate)
    {
        UpdateTimestamp();
    }

    public void AssessLateFees(DateTime asOfDate, decimal lateFeeAmount)
    {
        CurrentLateFeeBalance += lateFeeAmount;
        UpdateTimestamp();
    }
}
