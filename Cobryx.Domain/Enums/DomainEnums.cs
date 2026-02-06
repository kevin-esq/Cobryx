namespace Cobryx.Domain.Enums;

public enum InterestType { Simple, Flat, Amortized }
public enum PenaltyType { Daily, FixedOneTime }
public enum PaymentPriority { InterestFirst, CapitalFirst, Proportional }
public enum DocumentType { INE, RFC, CURP, PASSPORT, OTHER }
public enum CreditStatus { Active, Paid, Overdue, Defaulted, Terminated }
public enum InstallmentStatus { Pending, Partial, Paid, Overdue }
public enum PaymentFrequency { Daily, Weekly, BiWeekly, Monthly, SinglePayment }
public enum InvoiceStatus { Draft, Issued, Partial, Paid, Overdue, Cancelled }
public enum PaymentStatus { Pending, Processing, Completed, Cancelled, Failed }
public enum SubscriptionStatus { Active, Trial, PastDue, Terminated, Cancelled }
public enum MetricType { Invoices, Users, Storage }
public enum BillingAlertType { LimitReached, RenewalNear, PaymentFailed }
public enum DiscountType { Flat, Percentage }
public enum CancellationReason { Price, MissingFeatures, Bugs, BetterAlternative, Other }
public enum SecurityTokenType { EmailVerification, PasswordReset }
