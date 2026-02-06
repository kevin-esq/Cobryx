namespace Cobryx.Api.Errors.Definitions;

public class ErrorDefinition
{
    public int StatusCode { get; }
    public int NumericCode { get; }

    public ErrorDefinition(int statusCode, int numericCode)
    {
        StatusCode = statusCode;
        NumericCode = numericCode;
    }
}
