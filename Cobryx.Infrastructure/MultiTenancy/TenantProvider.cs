using Cobryx.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Cobryx.Infrastructure.MultiTenancy;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string TenantHeader = "X-Tenant-Id";

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? GetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        // Try to get from Header
        if (httpContext.Request.Headers.TryGetValue(TenantHeader, out var tenantIdStr))
        {
            if (Guid.TryParse(tenantIdStr, out var tenantId))
            {
                return tenantId;
            }
        }

        // Future: Try to get from Claims (JWT)
        // var claim = httpContext.User.Claims.FirstOrDefault(c => c.Type == "tenant_id");

        return null;
    }
}
