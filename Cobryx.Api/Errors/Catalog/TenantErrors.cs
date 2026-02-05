using Cobryx.Api.Errors.Definitions;

namespace Cobryx.Api.Errors.Catalog;

public static class TenantErrors
{
    public static readonly ErrorDefinition NotFound = new(404, 3001, "Tenant Not Found");
    public static readonly ErrorDefinition ContextMissing = new(401, 3002, "Tenant Context Missing");
    public static readonly ErrorDefinition OnboardingCompleted = new(400, 3003, "Onboarding Already Completed");
    public static readonly ErrorDefinition OnboardingRequired = new(403, 3004, "Onboarding Required");
}
