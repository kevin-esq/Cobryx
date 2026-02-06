using Cobryx.Domain.Common;
using Cobryx.Api.Errors.Definitions;
using Cobryx.Api.Errors.Catalog;

namespace Cobryx.Api.Errors.Mappers;

public static class ErrorMapper
{
    public static ErrorDefinition Map(string errorCode) => errorCode switch
    {
        DomainErrorCodes.Auth.InvalidCredentials => new(401, 1101),
        DomainErrorCodes.Auth.AccountLocked => new(403, 1102),
        DomainErrorCodes.Auth.EmailNotVerified => new(403, 1103),
        DomainErrorCodes.Auth.TokenExpired => new(401, 1104),
        DomainErrorCodes.Auth.NotAuthenticated => new(401, 1105),
        DomainErrorCodes.Auth.InvalidToken => new(401, 1106),
        DomainErrorCodes.Auth.TokenCompromised => new(403, 1107),
        DomainErrorCodes.Auth.SessionRevoked => new(401, 1108),

        DomainErrorCodes.User.NotFound => new(404, 4001),
        DomainErrorCodes.User.EmailAlreadyExists => new(409, 4002),
        DomainErrorCodes.User.NotRegistered => new(404, 4003),

        DomainErrorCodes.Customer.NotFound => new(404, 3001),
        DomainErrorCodes.Customer.Duplicate => new(409, 3002),

        DomainErrorCodes.Tenant.NotFound => new(404, 2001),
        DomainErrorCodes.Tenant.ContextMissing => TenantErrors.ContextMissing,
        DomainErrorCodes.Tenant.OnboardingRequired => new(403, 2003),
        DomainErrorCodes.Tenant.OnboardingCompleted => new(409, 2004),

        DomainErrorCodes.Financial.InvoiceInvalidStatusForPayment => FinancialErrors.InvoiceInvalidStatusForPayment,
        DomainErrorCodes.Financial.InvoiceCurrencyMismatch => FinancialErrors.InvoiceCurrencyMismatch,
        DomainErrorCodes.Financial.PaymentNotProcessing => FinancialErrors.PaymentNotProcessing,
        DomainErrorCodes.Financial.InvoiceNotFound => FinancialErrors.InvoiceNotFound,

        "CREDITS.NOT_FOUND" => CreditErrors.NotFound,
        "CREDITS.INSUFFICIENT" => CreditErrors.Insufficient,

        DomainErrorCodes.System.ValidationFailed => new(400, 1001),
        DomainErrorCodes.System.TooManyRequests => new(429, 1090),
        DomainErrorCodes.System.InternalError => new(500, 1000),


        _ => new(400, 1000)
    };
}
