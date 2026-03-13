namespace Cobryx.Domain.Lending.Enums;

public enum InterestType { Simple, Flat, Amortized }
public enum InterestMethod { DailyCompound, Simple, Fixed, Compound }
public enum CompoundingFrequency { Daily, Weekly, Monthly }
public enum DayCountBasis { Actual365, Actual360, Thirty360 }
public enum PenaltyType { Daily, FixedOneTime }
public enum PaymentPriority { InterestFirst, CapitalFirst, Proportional }
public enum CreditStatus { Active, Paid, Overdue, Defaulted, Terminated }
public enum InterestOrigin { Explicit, Implicit }
public enum PaymentFrequency { Daily, Weekly, BiWeekly, Monthly, SinglePayment, Custom }
public enum RoundingMode { None, ToNearest, ToTen, ToFifty, ToHundred }
public enum LoanOrigin { Platform, Embedded, Partner, CashLoan }
