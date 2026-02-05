using Cobryx.Api.Errors.Definitions;

namespace Cobryx.Api.Errors.Catalog;

public static class TenantErrors
{
    public static readonly ErrorDefinition NotFound = new(404, 3001);
    public static readonly ErrorDefinition ContextMissing = new(401, 3002);
    public static readonly ErrorDefinition OnboardingCompleted = new(400, 3003);
    public static readonly ErrorDefinition OnboardingRequired = new(403, 3004);
}
