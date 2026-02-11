using Cobryx.Api.Errors.Definitions;

namespace Cobryx.Api.Errors.Catalog;

public static class UserErrors
{
    public static readonly ErrorDefinition NotFound = new(404, 4001);
    public static readonly ErrorDefinition EmailAlreadyExists = new(409, 4002);
    public static readonly ErrorDefinition NotRegistered = new(404, 4003);
}
