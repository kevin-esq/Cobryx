namespace Cobryx.Domain.Identity.Enums;

public enum TenantOnboardingStatus { New, Pending, Verifying, Active, Restricted, Completed }
public enum InvitationStatus { Pending, Accepted, Expired, Cancelled, Revoked }
public enum SecurityTokenType { EmailVerification, PasswordReset }
public enum TenantStatus { Active, Suspended, Archived }
