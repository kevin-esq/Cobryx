using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Lending;

/// <summary>
/// Represents a single payment obligation within an amortization schedule.
/// </summary>
public class Installment : BaseEntity
{
    public Guid InstrumentId { get; private set; }
    public Guid CreditId => InstrumentId;
    public Guid LoanId => InstrumentId;
    public int Number { get; private set; }
    public int InstallmentNumber => Number;
    public DateTime DueDate { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public virtual BaseLendingInstrument Instrument { get; private set; } = null!;
    public Loan Loan => (Instrument as Loan)!;
    public Money TotalAmount { get; private set; }
    public Money PrincipalPart { get; private set; }
    public Money PrincipalPaid { get; private set; }
    public Money InterestPart { get; private set; }
    public Money InterestPaid { get; private set; }
    public Money LateInterestAmount { get; private set; }
    public Money LateInterestPaid { get; private set; }
    public Money RemainingBalance { get; private set; }
    public InstallmentStatus Status { get; private set; }

    public decimal PrincipalAmount => PrincipalPart.Amount;
    public decimal InterestAmount => InterestPart.Amount;
    public decimal LateFeeAmount => LateInterestAmount.Amount;
    public decimal LateFeePaid => LateInterestPaid.Amount;
    public decimal TotalPaid => PrincipalPaid.Amount + InterestPaid.Amount + LateFeePaid;
    public decimal RemainingAmount => RemainingBalance.Amount;

    public Money TotalDue => (PrincipalPart + InterestPart + LateInterestAmount) - (PrincipalPaid + InterestPaid + LateInterestPaid);

    private Installment()
    {
        TotalAmount = null!;
        PrincipalPart = null!;
        PrincipalPaid = null!;
        InterestPart = null!;
        InterestPaid = null!;
        LateInterestAmount = null!;
        LateInterestPaid = null!;
        RemainingBalance = null!;
    }

    public Installment(Guid instrumentId, int number, DateTime dueDate, Money principal, Money interest, Money balance)
    {
        InstrumentId = instrumentId;
        Number = number;
        DueDate = dueDate;
        PrincipalPart = principal;
        PrincipalPaid = Money.Zero(principal.Currency);
        InterestPart = interest;
        InterestPaid = Money.Zero(principal.Currency);
        LateInterestAmount = Money.Zero(principal.Currency);
        LateInterestPaid = Money.Zero(principal.Currency);
        TotalAmount = principal + interest;
        RemainingBalance = balance;
        Status = InstallmentStatus.Pending;
    }

    public Installment(Guid loanId, int installmentNumber, DateTime dueDate, decimal principalAmount, decimal interestAmount)
        : this(loanId, installmentNumber, dueDate, new Money(principalAmount, "USD"), new Money(interestAmount, "USD"), new Money(principalAmount + interestAmount, "USD"))
    {
    }

    public void SetLoanId(Guid loanId)
    {
        InstrumentId = loanId;
    }

    public decimal ApplyPayment(decimal amount)
    {
        decimal remaining = Math.Round(amount, 2);

        decimal lateDue = Math.Max(0, LateInterestAmount.Amount - LateInterestPaid.Amount);
        if (lateDue > 0 && remaining > 0)
        {
            decimal toPay = Math.Min(lateDue, remaining);
            LateInterestPaid = new Money(LateInterestPaid.Amount + toPay, LateInterestPaid.Currency);
            remaining -= toPay;
        }

        decimal interestDue = Math.Max(0, InterestPart.Amount - InterestPaid.Amount);
        if (interestDue > 0 && remaining > 0)
        {
            decimal toPay = Math.Min(interestDue, remaining);
            InterestPaid = new Money(InterestPaid.Amount + toPay, InterestPaid.Currency);
            remaining -= toPay;
        }

        decimal principalDue = Math.Max(0, PrincipalPart.Amount - PrincipalPaid.Amount);
        if (principalDue > 0 && remaining > 0)
        {
            decimal toPay = Math.Min(principalDue, remaining);
            PrincipalPaid = new Money(PrincipalPaid.Amount + toPay, PrincipalPaid.Currency);
            remaining -= toPay;
        }

        UpdateStatus();
        UpdateTimestamp();

        return remaining;
    }

    public decimal ApplyAllocation(decimal amount, PaymentApplicationType allocationType)
    {
        _ = allocationType;
        return ApplyPayment(amount);
    }

    private void UpdateStatus()
    {
        if (TotalDue.Amount <= 0)
        {
            if (Status != InstallmentStatus.Paid)
            {
                Status = InstallmentStatus.Paid;
                PaidAt = DateTime.UtcNow;
            }
        }
        else if (PrincipalPaid.Amount > 0 || InterestPaid.Amount > 0 || LateInterestPaid.Amount > 0)
        {
            Status = InstallmentStatus.Partial;
        }
    }

    public void ApplyLateInterest(Money amount)
    {
        LateInterestAmount = amount;
        TotalAmount = PrincipalPart + InterestPart + LateInterestAmount;
        UpdateStatus();
        UpdateTimestamp();
    }

    public void MarkAsPaid()
    {
        PrincipalPaid = PrincipalPart;
        InterestPaid = InterestPart;
        LateInterestPaid = LateInterestAmount;
        if (Status != InstallmentStatus.Paid)
        {
            Status = InstallmentStatus.Paid;
            PaidAt = DateTime.UtcNow;
        }
        UpdateTimestamp();
    }

    public void MarkAsOverdue(DateTime businessDate)
    {
        if ((Status == InstallmentStatus.Pending || Status == InstallmentStatus.Partial) && businessDate > DueDate)
        {
            Status = InstallmentStatus.Overdue;
            UpdateTimestamp();
        }
    }
}
