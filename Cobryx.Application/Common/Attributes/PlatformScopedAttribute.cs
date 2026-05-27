namespace Cobryx.Application.Common.Attributes;

/// <summary>
/// Platform or admin operation. May target a tenant by explicit id in the request
/// but does not use ITenantProvider from the authenticated tenant session.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class PlatformScopedAttribute : Attribute;
