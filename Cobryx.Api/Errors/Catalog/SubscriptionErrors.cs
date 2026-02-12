using Cobryx.Api.Errors.Definitions;

namespace Cobryx.Api.Errors.Catalog;

public static class SubscriptionErrors
{
    public static readonly ErrorDefinition NotFound = new(404, 7000);
    public static readonly ErrorDefinition LimitReached = new(403, 7001);
    public static readonly ErrorDefinition Expired = new(403, 7002);
    public static readonly ErrorDefinition Blocked = new(403, 7003);
    public static readonly ErrorDefinition DowngradeNotAllowed = new(400, 7004);
}
