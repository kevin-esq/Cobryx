using Cobryx.Api.Errors.Definitions;

namespace Cobryx.Api.Errors.Catalog;

public static class CustomerErrors
{
    public static readonly ErrorDefinition NotFound = new(404, 2001);
    public static readonly ErrorDefinition Duplicate = new(409, 2002);
}
