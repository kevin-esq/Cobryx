namespace Cobryx.Domain.Shared.Enums;

public enum CobryxModule
{
    Accounting,
    Lending,
    Payments,
    Invoicing,
    Identity,
    Auth,
    Risk,
    Messaging,
    Shared,
    System,
    Other
}

public enum MetricType { Invoices, Users, Storage }
public enum BillingAlertType { LimitReached, RenewalNear, PaymentFailed }
public enum DiscountType { Flat, Percentage }
public enum CancellationReason { Price, MissingFeatures, Bugs, BetterAlternative, Other }
public enum DocumentType { INE, RFC, CURP, PASSPORT, OTHER }
public enum ScanStatus { Pending, Success, Failed, ManualReview, PendingScan, Clean, Infected, ScanFailed }

public enum ChurnRisk
{
    Low = 0,
    Medium = 50,
    High = 90,
    Churned = 100
}

public enum ChurnType
{
    None = 0,
    Voluntary = 1,
    NonPayment = 2,
    Contraction = 3
}

public enum MRRChangeType
{
    None = 0,
    New = 1,
    Expansion = 2,
    Contraction = 3,
    Churn = 4,
    Reactivation = 5
}
