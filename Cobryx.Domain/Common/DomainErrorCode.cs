using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Cobryx.Api")]
[assembly: InternalsVisibleTo("Cobryx.Application")]
[assembly: InternalsVisibleTo("Cobryx.Infrastructure")]

namespace Cobryx.Domain.Common;

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
        public static readonly DomainErrorCode AccountLocked = new("AUTH.ACCOUNT_LOCKED");
        public static readonly DomainErrorCode EmailNotVerified = new("AUTH.EMAIL_NOT_VERIFIED");
        public static readonly DomainErrorCode InvalidCredentials = new("AUTH.INVALID_CREDENTIALS");
        public static readonly DomainErrorCode MfaRegistrationFailed = new("AUTH.MFA_REGISTRATION_FAILED");
        public static readonly DomainErrorCode TokenCompromised = new("AUTH.TOKEN.COMPROMISED");
        public static readonly DomainErrorCode TokenExpired = new("AUTH.TOKEN.EXPIRED");
        public static readonly DomainErrorCode SessionRevoked = new("AUTH.SESSION.REVOKED");
        public static readonly DomainErrorCode TokenNotActive = new("DOMAIN.TOKEN.NOT_ACTIVE");
    }

    public static class User
    {
        public static readonly DomainErrorCode NotRegistered = new("USER.NOT_REGISTERED");
        public static readonly DomainErrorCode EmailAlreadyExists = new("USER.EMAIL_ALREADY_EXISTS");
        public static readonly DomainErrorCode NotFound = new("USER.NOT_FOUND");
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
    }

    public static class ValueObjects
    {
        public static readonly DomainErrorCode InvalidEmailFormat = new("DOMAIN.INVALID_EMAIL_FORMAT");
        public static readonly DomainErrorCode DisposableEmail = new("DOMAIN.DISPOSABLE_EMAIL");
        public static readonly DomainErrorCode TaxIdInvalid = new("DOMAIN.TAX_ID.INVALID");
        public static readonly DomainErrorCode TaxIdRequired = new("DOMAIN.TAX_ID.REQUIRED");
        public static readonly DomainErrorCode InvalidCurrency = new("DOMAIN.INVALID_CURRENCY");
        public static readonly DomainErrorCode CurrencyMismatch = new("DOMAIN.CURRENCY_MISMATCH");
        public static readonly DomainErrorCode TaxIdFormatInvalid = new("DOMAIN.INVALID_TAX_ID_FORMAT");
    }

    public static class Legal
    {
        public static readonly DomainErrorCode ConsentRequired = new("DOMAIN.LEGAL.CONSENT_REQUIRED");
        public static readonly DomainErrorCode PrivacyPolicyRequired = new("DOMAIN.LEGAL.PRIVACY_POLICY_REQUIRED");
        public static readonly DomainErrorCode TermsRequired = new("DOMAIN.LEGAL.TERMS_REQUIRED");
        public static readonly DomainErrorCode TaxConsentRequired = new("DOMAIN.LEGAL.TAX_CONSENT_REQUIRED");
        public static readonly DomainErrorCode InvalidConsentVersion = new("DOMAIN.INVALID_CONSENT_VERSION");
        public static readonly DomainErrorCode IpAddressRequired = new("DOMAIN.IP_ADDRESS_REQUIRED");
        public static readonly DomainErrorCode UserAgentRequired = new("DOMAIN.USER_AGENT_REQUIRED");
    }

    public static class Documents
    {
        public static readonly DomainErrorCode FileSizeExceeded = new("DOMAIN.DOCUMENTS.FILE_SIZE_EXCEEDED");
        public static readonly DomainErrorCode VirusDetected = new("DOMAIN.DOCUMENTS.VIRUS_DETECTED");
    }

    public static class Invoicing
    {
        public static readonly DomainErrorCode InvoiceInvalidStatusForPayment = new("DOMAIN.INVOICE.INVALID_STATUS_FOR_PAYMENT");
        public static readonly DomainErrorCode InvoiceCurrencyMismatch = new("DOMAIN.INVOICE.CURRENCY_MISMATCH");
        public static readonly DomainErrorCode PaymentNotProcessing = new("DOMAIN.PAYMENT.NOT_PROCESSING");
        public static readonly DomainErrorCode InvoiceNotFound = new("DOMAIN.INVOICING.INVOICE_NOT_FOUND");
        public static readonly DomainErrorCode InvoiceNoItems = new("DOMAIN.INVOICE.NO_ITEMS");
        public static readonly DomainErrorCode InvoiceNotDraftAddItem = new("DOMAIN.INVOICE.NOT_DRAFT_ADD_ITEM");
        public static readonly DomainErrorCode InvoiceNotDraftRecalculate = new("DOMAIN.INVOICE.NOT_DRAFT_RECALCULATE");
        public static readonly DomainErrorCode InvoiceNotDraft = new("DOMAIN.INVOICE.NOT_DRAFT");
        public static readonly DomainErrorCode InvoiceInvalidStatusForCancel = new("DOMAIN.INVOICE.INVALID_STATUS_FOR_CANCEL");
        public static readonly DomainErrorCode InvoiceAllocationMismatch = new("DOMAIN.INVOICE.ALLOCATION_MISMATCH");

        public static readonly DomainErrorCode PaymentNotPending = new("DOMAIN.PAYMENT.NOT_PENDING");
        public static readonly DomainErrorCode PaymentCannotCancel = new("DOMAIN.PAYMENT.CANNOT_CANCEL");
        public static readonly DomainErrorCode PaymentInsufficientRefundableAmount = new("DOMAIN.PAYMENT.INSUFFICIENT_REFUNDABLE_AMOUNT");
        public static readonly DomainErrorCode PaymentNotCompletedCannotRefund = new("DOMAIN.PAYMENT.NOT_COMPLETED_CANNOT_REFUND");
        public static readonly DomainErrorCode PaymentInvalidStatusForChargeback = new("DOMAIN.PAYMENT.INVALID_STATUS_FOR_CHARGEBACK");
        public static readonly DomainErrorCode InvalidAllocationAmount = new("DOMAIN.INVALID_ALLOCATION_AMOUNT");
        
        public static readonly DomainErrorCode PaymentMethodRequired = new("DOMAIN.PAYMENT.METHOD_REQUIRED");
        public static readonly DomainErrorCode PaymentReferenceRequired = new("DOMAIN.PAYMENT.REFERENCE_REQUIRED");
        public static readonly DomainErrorCode PaymentProviderError = new("DOMAIN.PAYMENT.PROVIDER_ERROR");
        public static readonly DomainErrorCode InvalidPaymentMethod = new("DOMAIN.PAYMENT.INVALID_METHOD");
        public static readonly DomainErrorCode TaxRateRequired = new("DOMAIN.TAX.RATE_REQUIRED");
        public static readonly DomainErrorCode TaxNameRequired = new("DOMAIN.TAX.NAME_REQUIRED");
        public static readonly DomainErrorCode TaxNotFound = new("DOMAIN.TAX.NOT_FOUND");
        public static readonly DomainErrorCode TaxRateNegative = new("DOMAIN.TAX.RATE_NEGATIVE");
        
        public static readonly DomainErrorCode PaymentTenantMismatch = new("DOMAIN.PAYMENT.TENANT_MISMATCH");
        public static readonly DomainErrorCode PaymentCustomerMismatch = new("DOMAIN.PAYMENT.CUSTOMER_MISMATCH");
        
        public static readonly DomainErrorCode PaymentMethodNameRequired = new("DOMAIN.PAYMENT_METHOD.NAME_REQUIRED");
        public static readonly DomainErrorCode PaymentMethodCodeRequired = new("DOMAIN.PAYMENT_METHOD.CODE_REQUIRED");
    }

    public static class Credits
    {
        public static readonly DomainErrorCode AmountMustBePositive = new("DOMAIN.CREDIT.AMOUNT_MUST_BE_POSITIVE");
        public static readonly DomainErrorCode InsufficientFunds = new("DOMAIN.CREDIT.INSUFFICIENT_FUNDS");
        public static readonly DomainErrorCode NotFound = new("DOMAIN.CREDIT.NOT_FOUND");
        public static readonly DomainErrorCode Insufficient = new("CREDITS.INSUFFICIENT"); // Legacy/Custom exception alignment
        public static readonly DomainErrorCode GenericNotFound = new("CREDITS.NOT_FOUND"); // Legacy/Custom exception alignment
        public static readonly DomainErrorCode InvalidInstallmentsCount = new("DOMAIN.INVALID_INSTALLMENTS_COUNT");
        public static readonly DomainErrorCode CreditAlreadyPaid = new("DOMAIN.CREDIT_ALREADY_PAID");
        public static readonly DomainErrorCode InstallmentsAlreadyGenerated = new("DOMAIN.INSTALLMENTS_ALREADY_GENERATED");
    }

    public static class Products
    {
        public static readonly DomainErrorCode NameRequired = new("DOMAIN.PRODUCT.NAME_REQUIRED");
        public static readonly DomainErrorCode PriceRequired = new("DOMAIN.PRODUCT.PRICE_REQUIRED");
        public static readonly DomainErrorCode InvalidCategory = new("DOMAIN.PRODUCT.INVALID_CATEGORY");
        public static readonly DomainErrorCode NotFound = new("DOMAIN.PRODUCT.NOT_FOUND");
        public static readonly DomainErrorCode CodeRequired = new("DOMAIN.PRODUCT.CODE_REQUIRED");
        public static readonly DomainErrorCode GenericNameRequired = new("DOMAIN.NAME_REQUIRED");
        public static readonly DomainErrorCode NotALoanProduct = new("DOMAIN.NOT_A_LOAN_PRODUCT");
        public static readonly DomainErrorCode BasePriceRequired = new("DOMAIN.BASE_PRICE_REQUIRED");
    }

    public static class System
    {
        public static readonly DomainErrorCode InternalError = new("SYSTEM.INTERNAL_ERROR");
        public static readonly DomainErrorCode TooManyRequests = new("SYSTEM.TOO_MANY_REQUESTS");
        public static readonly DomainErrorCode ValidationFailed = new("VALIDATION.FAILED");
    }

    public static class Marketing
    {
        public static readonly DomainErrorCode CouponInvalid = new("DOMAIN.COUPON.INVALID");
        public static readonly DomainErrorCode InvalidCouponGeneral = new("DOMAIN.INVALID_COUPON");
    }

    public static class Api
    {
        public static readonly DomainErrorCode IdMismatch = new("API.ID_MISMATCH");
    }

    public static class Support
    {
        public static readonly DomainErrorCode TicketNotFound = new("SUPPORT.TICKET_NOT_FOUND");
    }

    internal static DomainErrorCode From(string value) => new(value);
}
