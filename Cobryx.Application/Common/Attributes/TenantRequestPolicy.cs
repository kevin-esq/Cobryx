using Cobryx.Application.Common.Interfaces;

using Concordia;

namespace Cobryx.Application.Common.Attributes;

/// <summary>
/// Central rules for MediatR request tenant policy classification.
/// </summary>
public static class TenantRequestPolicy
{
    public static bool IsMediatRRequest(Type type) =>
        !type.IsInterface
        && !type.IsAbstract
        && type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

    public static bool HasTenantScopedAttribute(Type type) =>
        type.IsDefined(typeof(TenantScopedAttribute), inherit: false);

    public static bool ImplementsIRequiresTenant(Type type) =>
        typeof(IRequiresTenant).IsAssignableFrom(type);

    public static bool HasPublicRequest(Type type) =>
        type.IsDefined(typeof(PublicRequestAttribute), inherit: false);

    public static bool HasTokenScoped(Type type) =>
        type.IsDefined(typeof(TokenScopedAttribute), inherit: false);

    public static bool HasWebhookSystem(Type type) =>
        type.IsDefined(typeof(WebhookSystemAttribute), inherit: false);

    public static bool HasPlatformScoped(Type type) =>
        type.IsDefined(typeof(PlatformScopedAttribute), inherit: false);

    public static int NonTenantClassificationCount(Type type)
    {
        var count = 0;
        if (HasPublicRequest(type)) count++;
        if (HasTokenScoped(type)) count++;
        if (HasWebhookSystem(type)) count++;
        if (HasPlatformScoped(type)) count++;
        return count;
    }

    public static bool IsTenantScoped(Type type) =>
        HasTenantScopedAttribute(type) && ImplementsIRequiresTenant(type);

    public static bool HasExplicitNonTenantClassification(Type type) =>
        NonTenantClassificationCount(type) > 0;

    /// <summary>
    /// Request has an explicit policy (tenant-scoped or classified non-tenant).
    /// Unclassified requests are architecture gaps.
    /// </summary>
    public static bool IsPolicyCovered(Type type) =>
        IsTenantScoped(type) || HasExplicitNonTenantClassification(type);

    public static bool IsUnclassifiedGap(Type type) =>
        IsMediatRRequest(type) && !IsPolicyCovered(type);

    public static string? GetPolicyLabel(Type type)
    {
        if (IsTenantScoped(type)) return "tenant-scoped";
        if (HasPublicRequest(type)) return "public";
        if (HasTokenScoped(type)) return "token-scoped";
        if (HasWebhookSystem(type)) return "webhook-system";
        if (HasPlatformScoped(type)) return "platform-scoped";
        return null;
    }
}
