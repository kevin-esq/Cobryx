using Asp.Versioning;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Application.Portal.Queries.GetCustomerPortalSummary;
using Cobryx.Domain.Common;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1.Portal;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/portal")]
[Tags("Customer Portal")]
public class PortalController : CobryxBaseController
{
    public PortalController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Gets a full summary for the customer portal: balances, active loans, and recent transactions.
    /// Ledger-backed for financial truth.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiSuccessResponse<CustomerPortalSummaryDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var customerIdClaim = User.FindFirst("customer_id")?.Value;
        var tenantIdClaim = User.FindFirst(CobryxClaimTypes.TenantId)?.Value;

        if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId) ||
            string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return Forbid();
        }

        // 1. BANK-GRADE: Security Isolation
        // We verify the query is requested for the authenticated customer AND tenant context.
        var result = await Sender.Send(new GetCustomerPortalSummaryQuery(customerId, tenantId), ct);
        return HandleResult(result);
    }
}
