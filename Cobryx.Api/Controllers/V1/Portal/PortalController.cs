using Asp.Versioning;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Application.Customer.Queries.GetCustomerStatement;
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
        var (customerId, tenantId) = GetCustomerContext();
        if (customerId == Guid.Empty) return Forbid();

        var result = await Sender.Send(new GetCustomerPortalSummaryQuery(customerId, tenantId), ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Generates a full financial statement for the customer.
    /// Includes loan balances and payment history.
    /// </summary>
    [HttpGet("statement")]
    [ProducesResponseType(typeof(ApiSuccessResponse<CustomerStatementDto>), 200)]
    public async Task<IActionResult> GetStatement(CancellationToken ct)
    {
        var (customerId, tenantId) = GetCustomerContext();
        if (customerId == Guid.Empty) return Forbid();

        var result = await Sender.Send(new GetCustomerStatementQuery(customerId, tenantId), ct);
        return HandleResult(result);
    }

    private (Guid CustomerId, Guid TenantId) GetCustomerContext()
    {
        var customerIdClaim = User.FindFirst("customer_id")?.Value;
        var tenantIdClaim = User.FindFirst(CobryxClaimTypes.TenantId)?.Value;

        if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId) ||
            string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return (Guid.Empty, Guid.Empty);
        }

        return (customerId, tenantId);
    }
}
