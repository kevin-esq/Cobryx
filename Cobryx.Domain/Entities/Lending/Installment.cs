using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Lending.Enums;
using Cobryx.Domain.Exceptions;

namespace Cobryx.Domain.Entities.Lending;

/// <summary>
/// Represents a single payment obligation within an amortization schedule.
/// </summary>
public class Installment : BaseEntity
{
    public Guid LoanId { get; private set; }
    public int InstallmentNumber { get; private set; }
    public DateTime DueDate { get; private set; }
    public decimal PrincipalAmount { get; private set; }
    public decimal InterestAmount { get; private set; }
    public decimal TotalAmount => PrincipalAmount + InterestAmount;
    public decimal PrincipalPaid { get; private set; }
    public decimal InterestPaid { get; private set; }
    public decimal LateFeeAmount { get; private set; }
    public decimal LateFeePaid { get; private set; }
    public decimal TotalPaid => PrincipalPaid + InterestPaid + LateFeePaid;
    public decimal RemainingAmount => TotalAmount + LateFeeAmount - TotalPaid;
    public InstallmentStatus Status { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public virtual Loan Loan { get; private set; } = null!;

    private Installment() { }

    public Installment(
        Guid loanId,
        int installmentNumber,
        DateTime dueDate,
        decimal principalAmount,
        decimal interestAmount)
    {
        if (installmentNumber <= 0)
            throw new DomainException(DomainErrorCode.Credits.InvalidInstallmentsCount);
        if (principalAmount < 0 || interestAmount < 0)
            throw new DomainException(DomainErrorCode.Common.InvalidAmount);

        LoanId = loanId;
        InstallmentNumber = installmentNumber;
        DueDate = dueDate;
        PrincipalAmount = principalAmount;
        InterestAmount = interestAmount;
        PrincipalPaid = 0;
        InterestPaid = 0;
        LateFeeAmount = 0;
        LateFeePaid = 0;
        Status = InstallmentStatus.Pending;
    }

    public void SetLoanId(Guid loanId)
    {
        if (LoanId != Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.GeneralError);
        LoanId = loanId;
    }

    public decimal ApplyAllocation(decimal amount, PaymentApplicationType allocationType)
    {
        if (Status == InstallmentStatus.Paid)
            throw new DomainException(DomainErrorCode.Loans.InstallmentAlreadyPaid);

        if (amount <= 0)
            return 0;

        decimal applied = 0;

        switch (allocationType)
        {
            case PaymentApplicationType.Principal:
                var principalRemaining = PrincipalAmount - PrincipalPaid;
                applied = Math.Min(amount, principalRemaining);
                PrincipalPaid += applied;
                break;

            case PaymentApplicationType.Interest:
                var interestRemaining = InterestAmount - InterestPaid;
                applied = Math.Min(amount, interestRemaining);
                InterestPaid += applied;
                break;

            case PaymentApplicationType.LateFees:
                applied = amount;
                LateFeePaid += applied;
                break;
        }

        RecalculateStatus();
        UpdateTimestamp();

        return applied;
    }

    public void RecalculateStatus()
    {
        var isPaid = PrincipalPaid >= PrincipalAmount && InterestPaid >= InterestAmount;

        if (isPaid)
        {
            Status = InstallmentStatus.Paid;
            PaidAt = DateTime.UtcNow;
        }
        else if (PrincipalPaid > 0 || InterestPaid > 0)
        {
            Status = DateTime.UtcNow.Date > DueDate
                ? InstallmentStatus.Overdue
                : InstallmentStatus.Partial;
        }
        else if (DateTime.UtcNow.Date > DueDate)
        {
            Status = InstallmentStatus.Overdue;
        }
        else
        {
            Status = InstallmentStatus.Pending;
        }
    }

    public void UpdateDueDate(DateTime newDueDate)
    {
        if (Status == InstallmentStatus.Paid)
            throw new DomainException(DomainErrorCode.Loans.CannotModifyPaidInstallment);

        DueDate = newDueDate;
        RecalculateStatus();
        UpdateTimestamp();
    }
}
