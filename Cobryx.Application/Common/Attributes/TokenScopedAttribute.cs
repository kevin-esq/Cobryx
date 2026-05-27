namespace Cobryx.Application.Common.Attributes;

/// <summary>
/// Public or semi-public flow scoped by an opaque token, not by authenticated tenant context.
/// Tenant must be resolved inside the handler from the persisted token/link record.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class TokenScopedAttribute : Attribute;
