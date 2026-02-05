namespace Cobryx.Api.Errors;

public static class CustomerErrors
{
    public static readonly ErrorDefinition NotFound = new(404, 2001, "Customer Not Found");
    public static readonly ErrorDefinition Duplicate = new(409, 2002, "Duplicate Customer");
}
