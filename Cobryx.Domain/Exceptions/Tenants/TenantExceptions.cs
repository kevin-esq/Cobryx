using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Tenants;

public class TenantNotFoundException : CobryxException
{
    public override string ErrorCode => "TENANT.NOT_FOUND";

    public TenantNotFoundException(Guid tenantId)
        : base($"Tenant with ID {tenantId} was not found.")
    {
        Metadata.Add("TenantId", tenantId);
    }

    public TenantNotFoundException(string message) : base(message) { }
}

public class TenantContextMissingException : CobryxException
{
    public override string ErrorCode => "TENANT.CONTEXT_MISSING";

    public TenantContextMissingException()
        : base("Tenant context is missing. Request requires 'X-Tenant-ID' header or valid session context.")
    {
    }
}

public class OnboardingCompletedException : CobryxException
{
    public override string ErrorCode => "TENANT.ONBOARDING_COMPLETED";
    public OnboardingCompletedException(string message = "Business onboarding already completed.") : base(message) { }
}

public class OnboardingRequiredException : CobryxException
{
    public override string ErrorCode => "TENANT.ONBOARDING_REQUIRED";
    public OnboardingRequiredException(string message = "Business onboarding is required.") : base(message) { }
}
