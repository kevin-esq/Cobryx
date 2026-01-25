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

        // 1. Check Request-level Cache
        if (httpContext.Items.TryGetValue("Cache_TenantId", out var cachedId))
        {
            return (Guid?)cachedId;
        }

        Guid? tenantId = null;

        // 2. Try to get from Header
        if (httpContext.Request.Headers.TryGetValue(TenantHeader, out var tenantIdStr))
        {
            if (Guid.TryParse(tenantIdStr, out var id))
            {
                tenantId = id;
            }
        }

        // 3. Cache for current request
        httpContext.Items["Cache_TenantId"] = tenantId;

        return tenantId;
    }
}
