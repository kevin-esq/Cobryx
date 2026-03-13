using Asp.Versioning;

using Cobryx.Api.Outcomes;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for managing customer credit lines, scoring assessments, and permanent financial facilities.
/// Orchestrates the establishment of revolving or fixed-term credit facilities.
/// </summary>
[Authorize(Policy = "CanCreateCredits")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/lending/credits")]
[Tags("Financial Core")]
public class CreditsController(ISender sender, Application.Common.Interfaces.ITenantProvider tenantProvider) : CobryxBaseController(sender)
{
    private readonly Application.Common.Interfaces.ITenantProvider _tenantProvider = tenantProvider;

    /// <summary>
    /// Establishes a new credit line facility for a customer.
    /// </summary>
    /// <param name="request">The credit line configuration, including Amount and Interest terms.</param>
    /// <remarks>
    /// The 'InterestRate' is typically an annual nominal rate (APR) unless specified otherwise by the product policy.
    ///
    /// Possible Outcomes:
    /// - CREDIT.CREATED: Credit line successfully established and active.
    /// - CREDIT.POLICY_VIOLATION: Validation failed or internal domain conflict.
    /// </remarks>
    /// <response code="201">Returns the identifier for the established credit facility.</response>
    /// <response code="400">Invalid parameters or incompatible credit policy.</response>
    /// <response code="422">Business rule violation (e.g., customer already has an active limit).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCreditRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Intentional Mapping: Public Intent -> Internal Domain Implementation
        var command = new Application.Credits.Commands.Create.CreateCreditCommand(
            tenantId.Value,
            request.CustomerId,
            request.Amount,
            request.Currency,
            request.InterestRate,
            Enum.Parse<Cobryx.Domain.Lending.Enums.InterestType>(request.InterestType, true),
            Enum.Parse<Cobryx.Domain.Lending.Enums.PaymentFrequency>(request.Frequency, true),
            request.InstallmentsCount,
            request.GraceDays,
            request.ProductId);

        var result = await Sender.Send(command);
        return HandleCreatedResult($"/api/v1/lending/credits/{result.Value}", result, CreditOutcomes.Created);
    }

    /// <summary>
    /// Searches and lists credit line facilities within the current tenant context.
    /// </summary>
    /// <param name="customerId">Optional filter to retrieve facilities for a specific customer.</param>
    /// <param name="page">Pagination index (1-based).</param>
    /// <param name="pageSize">Number of records per page.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - CREDIT.SEARCH.COMPLETED: Results retrieved successfully.
    /// </remarks>
    /// <response code="200">A paginated list of credit facilities.</response>
    [HttpGet]
    [Authorize(Policy = "CanViewCredits")]
    [ProducesResponseType(typeof(ApiSuccessResponse<Cobryx.Application.Common.Models.PaginatedList<CreditSummaryContract>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await Sender.Send(new Application.Credits.Queries.GetCredits.GetCreditsQuery(customerId, page, pageSize));

        if (!result.IsSuccess || result.Value == null)
        {
            return HandleResult(result, CreditOutcomes.SearchCompleted);
        }

        var mapped = new Application.Common.Models.PaginatedList<CreditSummaryContract>(
            [.. result.Value.Items.Select(c => new CreditSummaryContract(
                c.Id,
                c.CustomerId,
                c.CustomerName,
                c.PrincipalAmount,
                c.Currency,
                c.InterestRate,
                c.InstallmentsCount,
                c.Status.ToString(),
                c.StartDate,
                c.TotalPaid,
                c.RemainingBalance))],
            result.Value.TotalCount,
            result.Value.Page,
            result.Value.TotalPages);

        return Success(mapped, CreditOutcomes.SearchCompleted);
    }

    /// <summary>
    /// Retrieves full details for a specific credit line facility.
    /// </summary>
    /// <param name="id">The unique identifier of the credit facility.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - CREDIT.SEARCH.COMPLETED: Facility found and retrieved.
    /// - CREDIT.FAILED: Facility not found.
    /// </remarks>
    /// <response code="200">The credit facility details.</response>
    /// <response code="404">Target credit facility not found.</response>
    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewCredits")]
    [ProducesResponseType(typeof(ApiSuccessResponse<CreditSummaryContract>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Sender.Send(new Application.Credits.Queries.GetCreditById.GetCreditByIdQuery(id));

        if (!result.IsSuccess || result.Value == null)
        {
            return HandleResult(result, CreditOutcomes.SearchCompleted);
        }

        var totalPaid = result.Value.Payments?.Sum(p => p.Amount) ?? 0m;
        var totalDue = result.Value.Schedule?.Sum(i => i.TotalDue) ?? result.Value.PrincipalAmount;

        var mapped = new CreditSummaryContract(
            result.Value.Id,
            result.Value.CustomerId,
            result.Value.CustomerName,
            result.Value.PrincipalAmount,
            result.Value.Currency,
            result.Value.InterestRate,
            result.Value.InstallmentsCount,
            result.Value.Status.ToString(),
            result.Value.StartDate,
            totalPaid,
            totalDue - totalPaid);

        return Success(mapped, CreditOutcomes.SearchCompleted);
    }
}
