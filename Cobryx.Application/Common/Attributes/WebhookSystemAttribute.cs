namespace Cobryx.Application.Common.Attributes;

/// <summary>
/// Stripe or system webhook handler. Runs without authenticated tenant context;
/// tenant is derived from persisted payment/link/account data inside the handler.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class WebhookSystemAttribute : Attribute;
