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
        if (httpContext == null)
            return null;

        if (httpContext.Items.TryGetValue("Cache_TenantId", out var cachedId))
        {
            return (Guid?)cachedId;
        }

        Guid? tenantId = null;

        if (httpContext.Request.Headers.TryGetValue(TenantHeader, out var tenantIdStr))
        {
            if (Guid.TryParse(tenantIdStr, out var id))
            {
                tenantId = id;
            }
        }

        httpContext.Items["Cache_TenantId"] = tenantId;

        return tenantId;
    }

    public void SetTenantId(Guid tenantId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Items["Cache_TenantId"] = tenantId;
        }
    }
}
