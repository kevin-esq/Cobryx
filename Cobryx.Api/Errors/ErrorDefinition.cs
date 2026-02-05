namespace Cobryx.Api.Errors;

public record ErrorDefinition(int StatusCode, int NumericCode, string Title);
