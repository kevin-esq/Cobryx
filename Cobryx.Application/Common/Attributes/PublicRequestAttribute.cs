namespace Cobryx.Application.Common.Attributes;

/// <summary>
/// Pre-authentication or anonymous request. No tenant context from JWT.
/// Examples: login, signup, forgot/reset password, email verification, MFA assertion before session.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class PublicRequestAttribute : Attribute;
