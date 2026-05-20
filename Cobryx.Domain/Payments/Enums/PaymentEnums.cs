namespace Cobryx.Domain.Payments.Enums;

public enum PaymentStatus { Pending, Processing, Completed, Cancelled, Failed, Chargeback, Retrying }
public enum PaymentLinkStatus { Draft, Active, Processing, RequiresAction, Paid, Expired, Cancelled, Failed, ManualReview }
public enum SubscriptionStatus { Active, Trial, PastDue, Terminated, Cancelled }
public enum PlanTier { Free, Starter, Pro, Business }
