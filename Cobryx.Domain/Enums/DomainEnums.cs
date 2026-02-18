namespace Cobryx.Domain.Enums;

public enum InterestType { Simple, Flat, Amortized }
public enum PenaltyType { Daily, FixedOneTime }
public enum PaymentPriority { InterestFirst, CapitalFirst, Proportional }
public enum DocumentType { INE, RFC, CURP, PASSPORT, OTHER }
public enum CreditStatus { Active, Paid, Overdue, Defaulted, Terminated }
public enum InstallmentStatus { Pending, Partial, Paid, Overdue }
public enum PaymentFrequency { Daily, Weekly, BiWeekly, Monthly, SinglePayment }
/// <summary>Represents the lifecycle stage of a commercial invoice.</summary>
public enum InvoiceStatus { Draft, Issued, Partial, Paid, Overdue, Cancelled }

/// <summary>Status of a financial transaction.</summary>
public enum PaymentStatus { Pending, Processing, Completed, Cancelled, Failed, Chargeback }

/// <summary>State of a tenant subscription according to Stripe synchronization.</summary>
public enum SubscriptionStatus { Active, Trial, PastDue, Terminated, Cancelled }

public enum MetricType { Invoices, Users, Storage }
public enum BillingAlertType { LimitReached, RenewalNear, PaymentFailed }
public enum DiscountType { Flat, Percentage }

/// <summary>Reason provided by the user for cancelling their subscription.</summary>
public enum CancellationReason { Price, MissingFeatures, Bugs, BetterAlternative, Other }
public enum SecurityTokenType { EmailVerification, PasswordReset }
public enum PlanTier { Free, Starter, Pro, Business }
