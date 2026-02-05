using Cobryx.Domain.Common;
using Cobryx.Api.Errors.Definitions;
using Cobryx.Api.Errors.Catalog;

namespace Cobryx.Api.Errors.Mappers;

public static class ErrorMapper
{
    public static ErrorDefinition Map(string errorCode) => errorCode switch
    {
        // --- AUTH ---
        "AUTH.INVALID_CREDENTIALS" => AuthErrors.InvalidCredentials,
        "AUTH.ACCOUNT_LOCKED" => AuthErrors.AccountLocked,
        "AUTH.EMAIL_NOT_VERIFIED" => AuthErrors.EmailNotVerified,
        "AUTH.TOKEN.EXPIRED" => AuthErrors.TokenExpired,
        "AUTH.NOT_AUTHENTICATED" => AuthErrors.NotAuthenticated,
        "AUTH.TOKEN.INVALID" => AuthErrors.TokenInvalid,

        // --- CUSTOMERS ---
        "CUSTOMER.NOT_FOUND" => CustomerErrors.NotFound,
        "CUSTOMER.DUPLICATE" => CustomerErrors.Duplicate,

        // --- TENANTS ---
        "TENANT.NOT_FOUND" => TenantErrors.NotFound,
        "TENANT.CONTEXT_MISSING" => TenantErrors.ContextMissing,
        "TENANT.ONBOARDING_COMPLETED" => TenantErrors.OnboardingCompleted,
        "TENANT.ONBOARDING_REQUIRED" => TenantErrors.OnboardingRequired,

        // --- USERS ---
        "USER.NOT_FOUND" => UserErrors.NotFound,
        "USER.EMAIL_ALREADY_EXISTS" => UserErrors.EmailAlreadyExists,
        "USER.NOT_REGISTERED" => UserErrors.NotRegistered,

        // --- CREDITS ---
        "CREDITS.NOT_FOUND" => CreditErrors.NotFound,
        "CREDITS.INSUFFICIENT" => CreditErrors.Insufficient,

        // --- VALIDATION ---
        "VALIDATION.FAILED" => new(400, 1001, "Validation Failed"),

        // --- SYSTEM ---
        "SYSTEM.TOO_MANY_REQUESTS" => new(429, 1090, "Too Many Requests"),
        "SYSTEM.INTERNAL_ERROR" => new(500, 1000, "Internal Server Error"),

        // --- FALLBACK ---
        _ => new(400, 1000, "Bad Request")
    };
}
