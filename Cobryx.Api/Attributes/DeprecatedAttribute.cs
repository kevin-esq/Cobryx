namespace Cobryx.Api.Attributes;

/// <summary>
/// Marks an API action as deprecated.
/// Triggers the DeprecationHeaderMiddleware to include Deprecation and Sunset headers.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class DeprecatedAttribute : Attribute
{
    public string? SunsetDate { get; }
    public string? AlternativeUri { get; }

    public DeprecatedAttribute(string? sunsetDate = null, string? alternativeUri = null)
    {
        SunsetDate = sunsetDate;
        AlternativeUri = alternativeUri;
    }
}
