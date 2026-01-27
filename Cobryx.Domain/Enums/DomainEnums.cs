namespace Cobryx.Domain.Enums;

public enum InterestType { Simple, Flat, Amortized }
public enum PenaltyType { Daily, FixedOneTime }
public enum PaymentPriority { InterestFirst, CapitalFirst, Proportional }
public enum DocumentType { INE, RFC, CURP, PASSPORT, OTHER }
public enum CreditStatus { Active, Paid, Overdue, Defaulted, Terminated }
public enum InstallmentStatus { Pending, Partial, Paid, Overdue }
public enum PaymentFrequency { Daily, Weekly, BiWeekly, Monthly, SinglePayment }
