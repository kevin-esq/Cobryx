using Cobryx.Api.Errors.Definitions;

namespace Cobryx.Api.Errors.Catalog;

public static class CreditErrors
{
    public static readonly ErrorDefinition NotFound = new(404, 5001, "Credit Record Not Found");
    public static readonly ErrorDefinition Insufficient = new(400, 5002, "Insufficient Credits");
}
