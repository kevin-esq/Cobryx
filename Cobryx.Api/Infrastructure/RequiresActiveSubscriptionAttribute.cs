namespace Cobryx.Api.Infrastructure;

/// <summary>
/// Forces subscription validation even on read-only (GET) requests.
/// Use on expensive endpoints such as reports, exports, and analytics
/// that should be blocked when the tenant's subscription is expired.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequiresActiveSubscriptionAttribute : Attribute { }
