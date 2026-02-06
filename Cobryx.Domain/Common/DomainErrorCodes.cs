namespace Cobryx.Domain.Common;

public static class DomainErrorCodes
{
    public static class Auth
    {
        public const string NotAuthenticated = "AUTH.NOT_AUTHENTICATED";
        public const string InvalidToken = "AUTH.TOKEN.INVALID";
        public const string TokenMissing = "AUTH.TOKEN.MISSING";
        public const string MfaAlreadyEnabled = "AUTH.MFA.ALREADY_ENABLED";
        public const string InvalidMfaCode = "AUTH.MFA.INVALID_CODE";
        public const string AccountInactive = "AUTH.ACCOUNT_INACTIVE";
        public const string ExternalLoginFailed = "AUTH.EXTERNAL_LOGIN_FAILED";
        public const string AccountLocked = "AUTH.ACCOUNT_LOCKED";
        public const string EmailNotVerified = "AUTH.EMAIL_NOT_VERIFIED";
        public const string InvalidCredentials = "AUTH.INVALID_CREDENTIALS";
        public const string MfaRegistrationFailed = "AUTH.MFA_REGISTRATION_FAILED";
        public const string TokenCompromised = "AUTH.TOKEN.COMPROMISED";
        public const string TokenExpired = "AUTH.TOKEN.EXPIRED";
        public const string SessionRevoked = "AUTH.SESSION.REVOKED";
    }

    public static class User
    {
        public const string NotRegistered = "USER.NOT_REGISTERED";
        public const string EmailAlreadyExists = "USER.EMAIL_ALREADY_EXISTS";
        public const string NotFound = "USER.NOT_FOUND";
    }

    public static class Tenant
    {
        public const string NotFound = "TENANT.NOT_FOUND";
        public const string ContextMissing = "TENANT.CONTEXT_MISSING";
        public const string OnboardingCompleted = "TENANT.ONBOARDING_COMPLETED";
        public const string OnboardingRequired = "TENANT.ONBOARDING_REQUIRED";
    }

    public static class Customer
    {
        public const string NotFound = "CUSTOMER.NOT_FOUND";
        public const string Duplicate = "CUSTOMER.DUPLICATE";
    }

    public static class Common
    {
        public const string UnauthorizedContext = "COMMON.UNAUTHORIZED_CONTEXT";
        public const string EntityNotFound = "DOMAIN.ENTITY_NOT_FOUND";
        public const string SystemConfigurationError = "SYSTEM.CONFIGURATION_ERROR";
    }

    public static class ValueObjects
    {
        public const string InvalidEmailFormat = "DOMAIN.INVALID_EMAIL_FORMAT";
        public const string DisposableEmail = "DOMAIN.DISPOSABLE_EMAIL";
    }

    public static class Documents
    {
        public const string FileSizeExceeded = "DOMAIN.DOCUMENTS.FILE_SIZE_EXCEEDED";
        public const string VirusDetected = "DOMAIN.DOCUMENTS.VIRUS_DETECTED";
    }

    public static class Financial
    {
        public const string InvoiceInvalidStatusForPayment = "DOMAIN.INVOICE.INVALID_STATUS_FOR_PAYMENT";
        public const string InvoiceCurrencyMismatch = "DOMAIN.INVOICE.CURRENCY_MISMATCH";
        public const string PaymentNotProcessing = "DOMAIN.PAYMENT.NOT_PROCESSING";
        public const string InvoiceNotFound = "DOMAIN.FINANCIAL.INVOICE_NOT_FOUND";
        public const string InvoiceNoItems = "DOMAIN.INVOICE.NO_ITEMS";
        public const string InvoiceNotDraftAddItem = "DOMAIN.INVOICE.NOT_DRAFT_ADD_ITEM";
        public const string InvoiceNotDraftRecalculate = "DOMAIN.INVOICE.NOT_DRAFT_RECALCULATE";
    }

    public static class System
    {
        public const string InternalError = "SYSTEM.INTERNAL_ERROR";
        public const string TooManyRequests = "SYSTEM.TOO_MANY_REQUESTS";
        public const string ValidationFailed = "VALIDATION.FAILED";
    }
}
