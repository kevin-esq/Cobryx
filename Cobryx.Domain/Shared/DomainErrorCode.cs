using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Cobryx.Api")]
[assembly: InternalsVisibleTo("Cobryx.Application")]
[assembly: InternalsVisibleTo("Cobryx.Infrastructure")]
[assembly: InternalsVisibleTo("Cobryx.IntegrationTests")]

namespace Cobryx.Domain.Shared;

/// <summary>
/// Domain-wide structured error codes following the elite engineering standard.
/// Usage: Result.Failure(Error.Customer.NotFound)
/// </summary>
public sealed record DomainErrorCode
{
    public string Value { get; }

    private DomainErrorCode(string value) => Value = value;

    public static implicit operator string(DomainErrorCode code) => code.Value;

    public override string ToString() => Value;

    public static class Auth
    {
        public static readonly DomainErrorCode NotAuthenticated = new("AUTH.NOT_AUTHENTICATED");
        public static readonly DomainErrorCode InvalidToken = new("AUTH.TOKEN.INVALID");
        public static readonly DomainErrorCode TokenMissing = new("AUTH.TOKEN.MISSING");
        public static readonly DomainErrorCode MfaAlreadyEnabled = new("AUTH.MFA.ALREADY_ENABLED");
        public static readonly DomainErrorCode InvalidMfaCode = new("AUTH.MFA.INVALID_CODE");
        public static readonly DomainErrorCode AccountInactive = new("AUTH.ACCOUNT_INACTIVE");
        public static readonly DomainErrorCode ExternalLoginFailed = new("AUTH.EXTERNAL_LOGIN_FAILED");
        public static readonly DomainErrorCode ProviderNotSupported = new("AUTH.PROVIDER_NOT_SUPPORTED");
        public static readonly DomainErrorCode AccountLocked = new("AUTH.ACCOUNT_LOCKED");
        public static readonly DomainErrorCode EmailNotVerified = new("AUTH.EMAIL_NOT_VERIFIED");
        public static readonly DomainErrorCode InvalidCredentials = new("AUTH.INVALID_CREDENTIALS");
        public static readonly DomainErrorCode MfaRegistrationFailed = new("AUTH.MFA_REGISTRATION_FAILED");
        public static readonly DomainErrorCode TokenCompromised = new("AUTH.TOKEN.COMPROMISED");
        public static readonly DomainErrorCode TokenExpired = new("AUTH.TOKEN.EXPIRED");
        public static readonly DomainErrorCode SessionRevoked = new("AUTH.SESSION.REVOKED");
        public static readonly DomainErrorCode TokenNotActive = new("AUTH.TOKEN.NOT_ACTIVE");
        public static readonly DomainErrorCode MfaNotConfigured = new("AUTH.MFA.NOT_CONFIGURED");
        public static readonly DomainErrorCode InvalidRole = new("AUTH.INVALID_ROLE");
        public static readonly DomainErrorCode EmailMismatch = new("AUTH.EMAIL_MISMATCH");
        public static readonly DomainErrorCode InvitationNotFound = new("AUTH.INVITATION_NOT_FOUND");
        public static readonly DomainErrorCode InvalidState = new("AUTH.INVALID_STATE");
        public static readonly DomainErrorCode RoleNotFound = new("AUTH.ROLE_NOT_FOUND");
        public static readonly DomainErrorCode Forbidden = new("AUTH.FORBIDDEN");
    }

    public static class User
    {
        public static readonly DomainErrorCode NotFound = new("USER.NOT_FOUND");
        public static readonly DomainErrorCode NotRegistered = new("USER.NOT_REGISTERED");
        public static readonly DomainErrorCode EmailAlreadyExists = new("USER.EMAIL_ALREADY_EXISTS");
        public static readonly DomainErrorCode AlreadyExists = new("USER.ALREADY_EXISTS");
        public static readonly DomainErrorCode TenantIdRequired = new("DOMAIN.USER.TENANT_ID_REQUIRED");
        public static readonly DomainErrorCode FirstNameRequired = new("DOMAIN.USER.FIRST_NAME_REQUIRED");
        public static readonly DomainErrorCode LastNameRequired = new("DOMAIN.USER.LAST_NAME_REQUIRED");
        public static readonly DomainErrorCode EmailRequired = new("DOMAIN.USER.EMAIL_REQUIRED");
        public static readonly DomainErrorCode RoleIdRequired = new("DOMAIN.USER.ROLE_ID_REQUIRED");
        public static readonly DomainErrorCode PasswordHashRequired = new("DOMAIN.USER.PASSWORD_HASH_REQUIRED");
    }

    public static class Tenant
    {
        public static readonly DomainErrorCode NotFound = new("TENANT.NOT_FOUND");
        public static readonly DomainErrorCode ContextMissing = new("TENANT.CONTEXT_MISSING");
        public static readonly DomainErrorCode OnboardingCompleted = new("TENANT.ONBOARDING_COMPLETED");
        public static readonly DomainErrorCode OnboardingRequired = new("TENANT.ONBOARDING_REQUIRED");
        public static readonly DomainErrorCode PlanRequired = new("DOMAIN.TENANT.PLAN_REQUIRED");
        public static readonly DomainErrorCode BusinessNameRequired = new("DOMAIN.BUSINESS_NAME_REQUIRED");
    }

    public static class Customer
    {
        public static readonly DomainErrorCode NotFound = new("CUSTOMER.NOT_FOUND");
        public static readonly DomainErrorCode Duplicate = new("CUSTOMER.DUPLICATE");
        public static readonly DomainErrorCode BusinessNameRequired = new("DOMAIN.CUSTOMER.BUSINESS_NAME_REQUIRED");
        public static readonly DomainErrorCode TaxIdRequired = new("DOMAIN.CUSTOMER.TAX_ID_REQUIRED");
        public static readonly DomainErrorCode EmailRequired = new("DOMAIN.CUSTOMER.EMAIL_REQUIRED");
        public static readonly DomainErrorCode AddressRequired = new("DOMAIN.CUSTOMER.ADDRESS_REQUIRED");
        public static readonly DomainErrorCode FirstNameRequired = new("DOMAIN.FIRST_NAME_REQUIRED");
        public static readonly DomainErrorCode LastNameRequired = new("DOMAIN.LAST_NAME_REQUIRED");
        public static readonly DomainErrorCode PhoneRequired = new("DOMAIN.PHONE_REQUIRED");
        public static readonly DomainErrorCode CustomerIdRequired = new("DOMAIN.CUSTOMER_ID_REQUIRED");
    }

    public static class Common
    {
        public static readonly DomainErrorCode UnauthorizedContext = new("COMMON.UNAUTHORIZED_CONTEXT");
        public static readonly DomainErrorCode EntityNotFound = new("DOMAIN.ENTITY_NOT_FOUND");
        public static readonly DomainErrorCode SystemConfigurationError = new("SYSTEM.CONFIGURATION_ERROR");
        public static readonly DomainErrorCode GeneralError = new("DOMAIN.GENERAL_ERROR");
        public static readonly DomainErrorCode TenantIdRequired = new("DOMAIN.TENANT_ID_REQUIRED");
        public static readonly DomainErrorCode EntityNameRequired = new("DOMAIN.ENTITY_NAME_REQUIRED");
        public static readonly DomainErrorCode InvalidAmount = new("DOMAIN.INVALID_AMOUNT");
        public static readonly DomainErrorCode ReasonRequired = new("COMMON.REASON_REQUIRED");
    }

    public static class ValueObjects
    {
        public static readonly DomainErrorCode InvalidEmailFormat = new("DOMAIN.INVALID_EMAIL_FORMAT");
        public static readonly DomainErrorCode DisposableEmail = new("DOMAIN.DISPOSABLE_EMAIL");
        public static readonly DomainErrorCode TaxIdInvalid = new("DOMAIN.TAX_ID.INVALID");
        public static readonly DomainErrorCode TaxIdRequired = new("DOMAIN.TAX_ID.REQUIRED");
        public static readonly DomainErrorCode InvalidCurrency = new("DOMAIN.INVALID_CURRENCY");
        public static readonly DomainErrorCode CurrencyMismatch = new("DOMAIN.CURRENCY_MISMATCH");
        public static readonly DomainErrorCode TaxIdFormatInvalid = new("DOMAIN.TAX_ID.FORMAT_INVALID");
    }

    public static class Legal
    {
        public static readonly DomainErrorCode NotFound = new("DOMAIN.LEGAL.NOT_FOUND");
        public static readonly DomainErrorCode ConsentRequired = new("DOMAIN.LEGAL.CONSENT_REQUIRED");
        public static readonly DomainErrorCode InvalidConsentVersion = new("DOMAIN.LEGAL.INVALID_VERSION");
        public static readonly DomainErrorCode IpAddressRequired = new("DOMAIN.LEGAL.IP_REQUIRED");
        public static readonly DomainErrorCode UserAgentRequired = new("DOMAIN.LEGAL.UA_REQUIRED");
    }

    public static class Documents
    {
        public static readonly DomainErrorCode NotFound = new("DOMAIN.DOCUMENT.NOT_FOUND");
        public static readonly DomainErrorCode VerificationFailed = new("DOMAIN.DOCUMENT.VERIFICATION_FAILED");
        public static readonly DomainErrorCode FileSizeExceeded = new("DOMAIN.DOCUMENT.SIZE_EXCEEDED");
        public static readonly DomainErrorCode VirusDetected = new("DOMAIN.DOCUMENT.VIRUS_DETECTED");
        public static readonly DomainErrorCode ScanInProgress = new("DOMAIN.DOCUMENT.SCAN_IN_PROGRESS");
        public static readonly DomainErrorCode InvalidFileType = new("DOMAIN.DOCUMENT.INVALID_FILE_TYPE");
        public static readonly DomainErrorCode SuspiciousArchive = new("DOMAIN.DOCUMENT.SUSPICIOUS_ARCHIVE");
        public static readonly DomainErrorCode ScannerUnavailable = new("DOMAIN.DOCUMENT.SCANNER_UNAVAILABLE");
    }

    public static class Invoicing
    {
        public static readonly DomainErrorCode InvoiceNotFound = new("DOMAIN.INVOICING.INVOICE_NOT_FOUND");
        public static readonly DomainErrorCode InvoiceInvalidStatusForPayment = new("DOMAIN.INVOICE.INVALID_STATUS_FOR_PAYMENT");
        public static readonly DomainErrorCode InvoiceCurrencyMismatch = new("DOMAIN.INVOICE.CURRENCY_MISMATCH");
        public static readonly DomainErrorCode PaymentNotProcessing = new("DOMAIN.PAYMENT.NOT_PROCESSING");
        public static readonly DomainErrorCode PaymentNotFound = new("DOMAIN.PAYMENT.NOT_FOUND");
        public static readonly DomainErrorCode PaymentMethodNotFound = new("DOMAIN.PAYMENT_METHOD.NOT_FOUND");
        public static readonly DomainErrorCode PaymentMethodNameRequired = new("DOMAIN.PAYMENT_METHOD.NAME_REQUIRED");
        public static readonly DomainErrorCode PaymentMethodCodeRequired = new("DOMAIN.PAYMENT_METHOD.CODE_REQUIRED");
        public static readonly DomainErrorCode TaxNotFound = new("DOMAIN.TAX.NOT_FOUND");
        public static readonly DomainErrorCode PaymentTenantMismatch = new("DOMAIN.PAYMENT.TENANT_MISMATCH");
        public static readonly DomainErrorCode PaymentCustomerMismatch = new("DOMAIN.PAYMENT.CUSTOMER_MISMATCH");
        public static readonly DomainErrorCode PaymentNotPending = new("DOMAIN.PAYMENT.NOT_PENDING");
        public static readonly DomainErrorCode InvalidAllocationAmount = new("DOMAIN.PAYMENT.INVALID_ALLOCATION");
        public static readonly DomainErrorCode PaymentCannotCancel = new("DOMAIN.PAYMENT.CANNOT_CANCEL");
        public static readonly DomainErrorCode PaymentNotCompletedCannotRefund = new("DOMAIN.PAYMENT.NOT_COMPLETED_CANNOT_REFUND");
        public static readonly DomainErrorCode PaymentInsufficientRefundableAmount = new("DOMAIN.PAYMENT.INSUFFICIENT_REFUNDABLE");
        public static readonly DomainErrorCode PaymentInvalidStatusForChargeback = new("DOMAIN.PAYMENT.INVALID_STATUS_FOR_CHARGEBACK");
        public static readonly DomainErrorCode InvoiceAllocationMismatch = new("DOMAIN.INVOICE.ALLOCATION_MISMATCH");
        public static readonly DomainErrorCode InvoiceNotDraftAddItem = new("DOMAIN.INVOICE.NOT_DRAFT_ADD_ITEM");
        public static readonly DomainErrorCode InvoiceNotDraftRecalculate = new("DOMAIN.INVOICE.NOT_DRAFT_RECALCULATE");
        public static readonly DomainErrorCode InvoiceNotDraft = new("DOMAIN.INVOICE.NOT_DRAFT");
        public static readonly DomainErrorCode InvoiceNoItems = new("DOMAIN.INVOICE.NO_ITEMS");
        public static readonly DomainErrorCode InvoiceInvalidStatusForCancel = new("DOMAIN.INVOICE.INVALID_STATUS_FOR_CANCEL");
        public static readonly DomainErrorCode TaxNameRequired = new("DOMAIN.TAX.NAME_REQUIRED");
        public static readonly DomainErrorCode TaxRateNegative = new("DOMAIN.TAX.RATE_NEGATIVE");
        public static readonly DomainErrorCode TaxDeleteDefaultForbidden = new("DOMAIN.TAX.DELETE_DEFAULT_FORBIDDEN");
        public static readonly DomainErrorCode TaxConfigurationNotFound = new("DOMAIN.TAX_CONFIGURATION.NOT_FOUND");
        public static readonly DomainErrorCode PaymentMethodAlreadyExists = new("DOMAIN.PAYMENT_METHOD.ALREADY_EXISTS");
        public static readonly DomainErrorCode ConcurrencyConflict = new("DOMAIN.INVOICING.CONCURRENCY_CONFLICT");
    }

    public static class Credits
    {
        public static readonly DomainErrorCode AmountMustBePositive = new("DOMAIN.CREDIT.AMOUNT_MUST_BE_POSITIVE");
        public static readonly DomainErrorCode InsufficientFunds = new("DOMAIN.CREDIT.INSUFFICIENT_FUNDS");
        public static readonly DomainErrorCode NotFound = new("DOMAIN.CREDIT.NOT_FOUND");
        public static readonly DomainErrorCode InvalidInstallmentsCount = new("DOMAIN.CREDIT.INVALID_INSTALLMENTS_COUNT");
        public static readonly DomainErrorCode CreditAlreadyPaid = new("DOMAIN.CREDIT.ALREADY_PAID");
        public static readonly DomainErrorCode InstallmentsAlreadyGenerated = new("DOMAIN.CREDIT.INSTALLMENTS_GENERATED");
    }

    public static class Products
    {
        public static readonly DomainErrorCode NameRequired = new("DOMAIN.PRODUCT.NAME_REQUIRED");
        public static readonly DomainErrorCode PriceRequired = new("DOMAIN.PRODUCT.PRICE_REQUIRED");
        public static readonly DomainErrorCode InvalidCategory = new("DOMAIN.PRODUCT.INVALID_CATEGORY");
        public static readonly DomainErrorCode NotFound = new("DOMAIN.PRODUCT.NOT_FOUND");
        public static readonly DomainErrorCode CodeRequired = new("DOMAIN.PRODUCT.CODE_REQUIRED");
        public static readonly DomainErrorCode GenericNameRequired = new("DOMAIN.PRODUCT.GENERIC_NAME_REQUIRED");
        public static readonly DomainErrorCode NotALoanProduct = new("DOMAIN.PRODUCT.NOT_A_LOAN");
        public static readonly DomainErrorCode BasePriceRequired = new("DOMAIN.PRODUCT.BASE_PRICE_REQUIRED");
    }

    public static class Loans
    {
        public static readonly DomainErrorCode NotFound = new("DOMAIN.LOAN.NOT_FOUND");
        public static readonly DomainErrorCode AlreadyClosed = new("DOMAIN.LOAN.ALREADY_CLOSED");
        public static readonly DomainErrorCode InstallmentNotFound = new("DOMAIN.INSTALLMENT.NOT_FOUND");
        public static readonly DomainErrorCode InstallmentAlreadyPaid = new("DOMAIN.INSTALLMENT.ALREADY_PAID");
        public static readonly DomainErrorCode AgreementAlreadySigned = new("DOMAIN.LOAN.AGREEMENT_SIGNED");
        public static readonly DomainErrorCode AgreementImmutableAfterSigning = new("DOMAIN.LOAN.AGREEMENT_LOCKED");
        public static readonly DomainErrorCode AgreementNotFound = new("DOMAIN.LOAN.AGREEMENT_NOT_FOUND");
        public static readonly DomainErrorCode CreditSaleInvalidPricing = new("DOMAIN.LOAN.INVALID_PRICING");
        public static readonly DomainErrorCode CannotModifyPaidInstallment = new("DOMAIN.LOAN.INSTALLMENT_PAID_LOCKED");
        public static readonly DomainErrorCode InvalidPaymentAmount = new("DOMAIN.LOAN.INVALID_PAYMENT_AMOUNT");
        public static readonly DomainErrorCode InvalidInterestPolicy = new("DOMAIN.LOAN.INVALID_INTEREST_POLICY");
        public static readonly DomainErrorCode InvalidLateFeePolicy = new("DOMAIN.LOAN.INVALID_LATE_FEE_POLICY");
        public static readonly DomainErrorCode CreditSaleNotFound = new("DOMAIN.LOAN.CREDIT_SALE_NOT_FOUND");
        public static readonly DomainErrorCode DuplicateAccrual = new("DOMAIN.LOAN.DUPLICATE_ACCRUAL");
    }

    public static class Subscription
    {
        public static readonly DomainErrorCode LimitExceeded = new("DOMAIN.SUBSCRIPTION.LIMIT_EXCEEDED");
        public static readonly DomainErrorCode PlanRequired = new("DOMAIN.SUBSCRIPTION.PLAN_REQUIRED");
        public static readonly DomainErrorCode FeatureLocked = new("DOMAIN.SUBSCRIPTION.FEATURE_LOCKED");
        public static readonly DomainErrorCode LimitReached = new("DOMAIN.SUBSCRIPTION.LIMIT_REACHED");
        public static readonly DomainErrorCode Expired = new("DOMAIN.SUBSCRIPTION.EXPIRED");
        public static readonly DomainErrorCode Blocked = new("DOMAIN.SUBSCRIPTION.BLOCKED");
        public static readonly DomainErrorCode NotFound = new("DOMAIN.SUBSCRIPTION.NOT_FOUND");
        public static readonly DomainErrorCode PlanNotFound = new("DOMAIN.SUBSCRIPTION.PLAN_NOT_FOUND");
        public static readonly DomainErrorCode AlreadyCancelled = new("DOMAIN.SUBSCRIPTION.ALREADY_CANCELLED");
        public static readonly DomainErrorCode DowngradeNotAllowed = new("DOMAIN.SUBSCRIPTION.DOWNGRADE_FORBIDDEN");
        public static readonly DomainErrorCode NotActiveInStripe = new("DOMAIN.SUBSCRIPTION.NOT_ACTIVE_IN_STRIPE");
        public static readonly DomainErrorCode PlanNotBillable = new("DOMAIN.SUBSCRIPTION.PLAN_NOT_BILLABLE");
    }

    public static class Support
    {
        public static readonly DomainErrorCode TicketNotFound = new("DOMAIN.SUPPORT.TICKET_NOT_FOUND");
        public static readonly DomainErrorCode TicketAlreadyClosed = new("DOMAIN.SUPPORT.TICKET_CLOSED");
    }

    public static class Marketing
    {
        public static readonly DomainErrorCode CouponNotFound = new("DOMAIN.MARKETING.COUPON_NOT_FOUND");
        public static readonly DomainErrorCode CouponExpired = new("DOMAIN.MARKETING.COUPON_EXPIRED");
        public static readonly DomainErrorCode InvalidCouponGeneral = new("DOMAIN.MARKETING.COUPON_INVALID");
    }

    public static class Webhooks
    {
        public static readonly DomainErrorCode EventAlreadyProcessed = new("DOMAIN.WEBHOOK.ALREADY_PROCESSED");
        public static readonly DomainErrorCode InvalidSignature = new("DOMAIN.WEBHOOK.INVALID_SIGNATURE");
        public static readonly DomainErrorCode EventNotFound = new("DOMAIN.WEBHOOK.NOT_FOUND");
        public static readonly DomainErrorCode InvalidDataFormat = new("DOMAIN.WEBHOOK.INVALID_DATA_FORMAT");
        public static readonly DomainErrorCode MissingTransactionId = new("DOMAIN.WEBHOOK.MISSING_TRANSACTION_ID");
        public static readonly DomainErrorCode ParserNotFound = new("DOMAIN.WEBHOOK.PARSER_NOT_FOUND");
    }

    public static class Stripe
    {
        public static readonly DomainErrorCode NoStripeCustomer = new("DOMAIN.STRIPE.NO_STRIPE_CUSTOMER");
    }

    public static class Accounting
    {
        public static readonly DomainErrorCode TransactionNotFound = new("ACCOUNTING.TRANSACTION_NOT_FOUND");
        public static readonly DomainErrorCode CannotReverseReversal = new("ACCOUNTING.CANNOT_REVERSE_REVERSAL");
    }

    public static class System
    {
        public static readonly DomainErrorCode InternalError = new("SYSTEM.INTERNAL_ERROR");
        public static readonly DomainErrorCode TooManyRequests = new("SYSTEM.TOO_MANY_REQUESTS");
        public static readonly DomainErrorCode ValidationFailed = new("VALIDATION.FAILED");
        public static readonly DomainErrorCode NotAllowed = new("SYSTEM.NOT_ALLOWED");
        public static readonly DomainErrorCode ServiceUnavailable = new("SYSTEM.SERVICE_UNAVAILABLE");
    }

    public static class PaymentLink
    {
        public static readonly DomainErrorCode NotFound = new("DOMAIN.PAYMENT_LINK.NOT_FOUND");
        public static readonly DomainErrorCode Expired = new("DOMAIN.PAYMENT_LINK.EXPIRED");
        public static readonly DomainErrorCode AlreadyPaid = new("DOMAIN.PAYMENT_LINK.ALREADY_PAID");
        public static readonly DomainErrorCode LimitReached = new("DOMAIN.PAYMENT_LINK.LIMIT_REACHED");
        public static readonly DomainErrorCode InvalidStatus = new("DOMAIN.PAYMENT_LINK.INVALID_STATUS");
        public static readonly DomainErrorCode ConnectNotActive = new("DOMAIN.PAYMENT_LINK.CONNECT_NOT_ACTIVE");
    }

    internal static DomainErrorCode From(string value) => new(value);
}

/// <summary>
/// Domain error entry point for elite engineering standard.
/// Example: Error.Customer.NotFound
/// </summary>
public static class Error
{
    public static class Auth
    {
        public static DomainErrorCode NotAuthenticated => DomainErrorCode.Auth.NotAuthenticated;
        public static DomainErrorCode InvalidCredentials => DomainErrorCode.Auth.InvalidCredentials;
    }

    public static class Customer
    {
        public static DomainErrorCode NotFound => DomainErrorCode.Customer.NotFound;
    }

    public static class System
    {
        public static DomainErrorCode InternalError => DomainErrorCode.System.InternalError;
    }
}
