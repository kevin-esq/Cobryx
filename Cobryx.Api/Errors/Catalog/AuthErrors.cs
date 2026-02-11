using Cobryx.Api.Errors.Definitions;

namespace Cobryx.Api.Errors.Catalog;

public static class AuthErrors
{
    public static readonly ErrorDefinition InvalidCredentials = new(401, 1101);
    public static readonly ErrorDefinition AccountLocked = new(403, 1102);
    public static readonly ErrorDefinition EmailNotVerified = new(403, 1103);
    public static readonly ErrorDefinition TokenExpired = new(401, 1104);
    public static readonly ErrorDefinition NotAuthenticated = new(401, 1105);
    public static readonly ErrorDefinition TokenInvalid = new(401, 1106);
    public static readonly ErrorDefinition TokenCompromised = new(403, 1107);
    public static readonly ErrorDefinition SessionRevoked = new(401, 1108);
}
