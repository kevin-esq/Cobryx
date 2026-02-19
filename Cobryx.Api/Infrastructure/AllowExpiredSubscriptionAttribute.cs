namespace Cobryx.Api.Infrastructure;

/// <summary>
/// Marks an endpoint as accessible regardless of the tenant's subscription status.
/// Use on billing, upgrade, cancel, and portal endpoints that must remain reachable
/// even when the tenant's subscription is expired or terminated.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowExpiredSubscriptionAttribute : Attribute { }
