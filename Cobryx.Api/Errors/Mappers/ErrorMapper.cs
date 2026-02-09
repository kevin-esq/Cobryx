using Cobryx.Domain.Common;
using Cobryx.Api.Errors.Definitions;
using Cobryx.Api.Errors.Catalog;

namespace Cobryx.Api.Errors.Mappers;

public static class ErrorMapper
{
    public static ErrorDefinition Map(DomainErrorCode errorCode) => errorCode switch
    {
        _ when errorCode == DomainErrorCode.Auth.InvalidCredentials => new(401, 1101),
        _ when errorCode == DomainErrorCode.Auth.AccountLocked => new(403, 1102),
        _ when errorCode == DomainErrorCode.Auth.EmailNotVerified => new(403, 1103),
        _ when errorCode == DomainErrorCode.Auth.TokenExpired => new(401, 1104),
        _ when errorCode == DomainErrorCode.Auth.NotAuthenticated => new(401, 1105),
        _ when errorCode == DomainErrorCode.Auth.InvalidToken => new(401, 1106),
        _ when errorCode == DomainErrorCode.Auth.TokenCompromised => new(403, 1107),
        _ when errorCode == DomainErrorCode.Auth.SessionRevoked => new(401, 1108),

        _ when errorCode == DomainErrorCode.User.NotFound => new(404, 4001),
        _ when errorCode == DomainErrorCode.User.EmailAlreadyExists => new(409, 4002),
        _ when errorCode == DomainErrorCode.User.NotRegistered => new(404, 4003),

        _ when errorCode == DomainErrorCode.Customer.NotFound => new(404, 3001),
        _ when errorCode == DomainErrorCode.Customer.Duplicate => new(409, 3002),

        _ when errorCode == DomainErrorCode.Tenant.NotFound => new(404, 2001),
        _ when errorCode == DomainErrorCode.Tenant.ContextMissing => TenantErrors.ContextMissing,
        _ when errorCode == DomainErrorCode.Tenant.OnboardingRequired => new(403, 2003),
        _ when errorCode == DomainErrorCode.Tenant.OnboardingCompleted => new(409, 2004),

        _ when errorCode == DomainErrorCode.Invoicing.InvoiceInvalidStatusForPayment => InvoicingErrors.InvoiceInvalidStatusForPayment,
        _ when errorCode == DomainErrorCode.Invoicing.InvoiceCurrencyMismatch => InvoicingErrors.InvoiceCurrencyMismatch,
        _ when errorCode == DomainErrorCode.Invoicing.PaymentNotProcessing => InvoicingErrors.PaymentNotProcessing,
        _ when errorCode == DomainErrorCode.Invoicing.InvoiceNotFound => InvoicingErrors.InvoiceNotFound,
        _ when errorCode == DomainErrorCode.Invoicing.PaymentInsufficientRefundableAmount => new(400, 5005),
        _ when errorCode == DomainErrorCode.Invoicing.PaymentNotCompletedCannotRefund => new(400, 5006),
        _ when errorCode == DomainErrorCode.Invoicing.PaymentInvalidStatusForChargeback => new(400, 5007),

        _ when errorCode == DomainErrorCode.Credits.NotFound => CreditErrors.NotFound,
        _ when errorCode == DomainErrorCode.Credits.InsufficientFunds => CreditErrors.Insufficient,

        _ when errorCode == DomainErrorCode.System.ValidationFailed => new(400, 1001),
        _ when errorCode == DomainErrorCode.System.TooManyRequests => new(429, 1090),
        _ when errorCode == DomainErrorCode.System.InternalError => new(500, 1000),

        _ => new(400, 1000)
    };
}
