namespace Cobryx.Api.Errors.Definitions;

public class ErrorDefinition(int statusCode, int numericCode)
{
    public int StatusCode { get; } = statusCode;
    public int NumericCode { get; } = numericCode;
}
