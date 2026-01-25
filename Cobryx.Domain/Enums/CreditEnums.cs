namespace Cobryx.Domain.Enums;

public enum CreditStatus
{
    Active,
    Paid,
    Defaulted,
    Cancelled
}

public enum PaymentFrequency
{
    Daily,
    Weekly,
    BiWeekly, // Fortnightly
    Monthly,
    SinglePayment // At the end
}

public enum InstallmentStatus
{
    Pending,
    Paid,
    Partial,
    Overdue
}
