namespace Cobryx.Api.Errors;

public static class AuthErrors
{
    public static readonly ErrorDefinition InvalidCredentials = new(401, 1101, "Invalid Credentials");
    public static readonly ErrorDefinition AccountLocked = new(403, 1102, "Account Locked");
    public static readonly ErrorDefinition EmailNotVerified = new(403, 1103, "Email Not Verified");
    public static readonly ErrorDefinition TokenExpired = new(401, 1104, "Token Expired");
    public static readonly ErrorDefinition NotAuthenticated = new(401, 1105, "Not Authenticated");
    public static readonly ErrorDefinition TokenInvalid = new(401, 1106, "Invalid Token");
}
