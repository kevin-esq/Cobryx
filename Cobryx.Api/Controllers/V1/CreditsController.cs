using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Api.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Models;
using Cobryx.Application.Credits.Commands.Create;
using Cobryx.Application.Credits.Common;
using Cobryx.Application.Credits.Queries.GetCreditById;
using Cobryx.Application.Credits.Queries.GetCredits;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ApiErrorResponse = Cobryx.Api.Contracts.V1.Common.ApiErrorResponse;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for managing customer credit lines, scoring assessments, and permanent financial facilities.
/// Orchestrates the establishment of revolving or fixed-term credit facilities.
/// </summary>
[Authorize(Policy = "CanCreateCredits")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/credits")]
[Tags("Lending")]
public class CreditsController(ISender sender, ITenantProvider tenantProvider, IApiLinkGenerator linkGenerator) : CobryxBaseController(sender)
{
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
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> Create([FromBody] CreateCreditRequest request)
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        if (tenantId == null)
            return Unauthorized();

        var command = new CreateCreditCommand(
            tenantId.Value,
            request.CustomerId,
            request.Amount,
            request.Currency,
            request.InterestRate,
            Enum.Parse<InterestType>(request.InterestType, true),
            Enum.Parse<PaymentFrequency>(request.Frequency, true),
            request.InstallmentsCount,
            request.GraceDays,
            request.ProductId);

        Result<Guid> result = await Sender.Send(command);
        return HandleCreatedResult(linkGenerator.GetCreditUrl(result.Value), result, CreditOutcomes.Created);
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
    [ProducesResponseType(
        typeof(Contracts.V1.Common.ApiSuccessResponse<PaginatedList<CreditSummaryContract>>),
        200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> GetAll([FromQuery] Guid? customerId, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        Result<PaginatedList<CreditDto>> result = await Sender.Send(new GetCreditsQuery(customerId, page, pageSize));

        if (!result.IsSuccess || result.Value == null)
        {
            return HandleResult(result, CreditOutcomes.SearchCompleted);
        }

        var mapped = new PaginatedList<CreditSummaryContract>(
            [
                .. result.Value.Items.Select(c => new CreditSummaryContract(
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
                    c.RemainingBalance))
            ],
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
    [HttpGet("{id}", Name = "GetCredit")]
    [Authorize(Policy = "CanViewCredits")]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiSuccessResponse<CreditSummaryContract>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetCredit(Guid id)
    {
        Result<CreditDetailDto> result = await Sender.Send(new GetCreditByIdQuery(id));

        if (!result.IsSuccess || result.Value == null)
        {
            return HandleResult(result, CreditOutcomes.SearchCompleted);
        }

        var totalPaid = result.Value.Payments.Sum(p => p.Amount);
        var totalDue = result.Value.Schedule.Sum(i => i.TotalDue);

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
