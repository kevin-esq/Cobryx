using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Api.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Models;
using Cobryx.Application.Customers.Commands.Create;
using Cobryx.Application.Customers.Commands.Delete;
using Cobryx.Application.Customers.Commands.Update;
using Cobryx.Application.Customers.Common;
using Cobryx.Application.Customers.Queries.GetCustomerById;
using Cobryx.Application.Customers.Queries.GetCustomers;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using AddressContract = Cobryx.Api.Contracts.V1.Common.AddressContract;
using ApiErrorResponse = Cobryx.Api.Contracts.V1.Common.ApiErrorResponse;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Manages the lifecycle of customer records, including profiles, identity validation, and contact information.
/// </summary>
[Authorize(Policy = "CanCreateCustomers")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers")]
[Tags("Customers")]
public class CustomersController(ISender sender, ITenantProvider tenantProvider, IApiLinkGenerator linkGenerator)
    : CobryxBaseController(sender)
{
    /// <summary>
    /// Registers a new customer within the tenant context.
    /// </summary>
    /// <param name="request">Customer data including identity documents and address details.</param>
    /// <remarks>
    /// Address fields are validated against geographical standards. Identity documents must follow the specified type patterns.
    ///
    /// Possible outcomes:
    /// - `CUSTOMER.CREATED`: Customer successfully registered.
    /// - `CUSTOMER.FAILED`: Validation failed (e.g., duplicate document number or invalid phone).
    /// </remarks>
    /// <response code="201">Returns the identifier of the registered customer.</response>
    /// <response code="400">Invalid or malformed request data.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="409">Document number already exists for this tenant.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        if (tenantId is null)
            return Unauthorized();

        var command = new CreateCustomerCommand(
            tenantId.Value,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            MapAddress(request.Address),
            MapDocument(request.Document));

        Result<Guid> result = await Sender.Send(command);
        return HandleCreatedResult(linkGenerator.GetCustomerUrl(result.Value), result, CustomerOutcomes.Created);
    }

    /// <summary>
    /// Returns a paginated list of customers, optionally filtered by a search term.
    /// </summary>
    /// <param name="searchTerm">Partial match against name, email, or document number.</param>
    /// <param name="page">Page index (1-based).</param>
    /// <param name="pageSize">Number of records per page.</param>
    /// <remarks>
    /// Possible outcomes:
    /// - `CUSTOMER.SEARCH.COMPLETED`: Search completed successfully.
    /// </remarks>
    /// <response code="200">Paginated list of customers.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    /// <response code="403">Insufficient permissions.</response>
    [HttpGet]
    [Authorize(Policy = "CanViewCustomers")]
    [ProducesResponseType(
        typeof(Contracts.V1.Common.ApiSuccessResponse<
            PaginatedList<CustomerSummaryContract>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        Result<PaginatedList<CustomerDto>>
            result = await Sender.Send(new GetCustomersQuery(searchTerm, page, pageSize));

        if (!result.IsSuccess || result.Value is null)
            return HandleResult(result, CustomerOutcomes.SearchCompleted);

        var mapped = new PaginatedList<CustomerSummaryContract>(
            [.. result.Value.Items.Select(MapToContract)],
            result.Value.TotalCount,
            result.Value.Page,
            result.Value.TotalPages);

        return Success(mapped, CustomerOutcomes.SearchCompleted);
    }

    /// <summary>
    /// Retrieves the complete profile for a specific customer.
    /// </summary>
    /// <param name="id">Unique identifier of the customer.</param>
    /// <remarks>
    /// Possible outcomes:
    /// - `CUSTOMER.SEARCH.COMPLETED`: Customer profile retrieved.
    /// - `CUSTOMER.FAILED`: Customer not found.
    /// </remarks>
    /// <response code="200">The requested customer profile.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="404">Customer not found.</response>
    [HttpGet("{id:guid}", Name = "GetCustomer")]
    [Authorize(Policy = "CanViewCustomers")]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiSuccessResponse<CustomerSummaryContract>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        Result<CustomerDto> result = await Sender.Send(new GetCustomerByIdQuery(id));

        if (!result.IsSuccess || result.Value is null)
            return HandleResult(result, CustomerOutcomes.SearchCompleted);

        return Success(MapToContract(result.Value), CustomerOutcomes.SearchCompleted);
    }

    /// <summary>
    /// Updates an existing customer's contact or profile details.
    /// </summary>
    /// <param name="id">Unique identifier of the customer to update.</param>
    /// <param name="request">Fields to update. Only provided values will be modified.</param>
    /// <remarks>
    /// Possible outcomes:
    /// - `CUSTOMER.UPDATED`: Changes persisted successfully.
    /// - `CUSTOMER.FAILED`: Validation failed or customer not found.
    /// </remarks>
    /// <response code="200">Customer updated successfully.</response>
    /// <response code="400">Invalid request data.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="404">Customer not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Contracts.V1.Common.ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var command = new UpdateCustomerCommand(
            id,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            MapAddress(request.Address),
            MapDocument(request.Document));

        Result result = await Sender.Send(command);
        return HandleResult(result, CustomerOutcomes.Updated);
    }

    /// <summary>
    /// Soft-deletes a customer, deactivating the record from active operations.
    /// </summary>
    /// <param name="id">Unique identifier of the customer to deactivate.</param>
    /// <remarks>
    /// Historical data (invoices, loans) remains associated with the record for audit purposes.
    ///
    /// Possible outcomes:
    /// - `CUSTOMER.DELETED`: Record successfully deactivated.
    /// - `CUSTOMER.FAILED`: Customer not found.
    /// </remarks>
    /// <response code="204">Customer deactivated successfully.</response>
    /// <response code="401">Missing or invalid authentication.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="404">Customer not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        Result result = await Sender.Send(new DeleteCustomerCommand(id));
        return HandleDeleteResult(result, CustomerOutcomes.Deleted);
    }

    private static Address? MapAddress(AddressContract? request) =>
        request is null
            ? null
            : new Address(
                request.Street,
                request.HouseNumber,
                request.ApartmentNumber,
                request.Neighborhood ?? string.Empty,
                request.PostalCode ?? string.Empty,
                request.City ?? string.Empty,
                request.State ?? string.Empty);

    private static IdentityDocument? MapDocument(IdentityDocumentContract? request) =>
        request is null ? null : new IdentityDocument(request.Type, request.Number);

    private static CustomerSummaryContract MapToContract(CustomerDto customer) =>
        new(
            customer.Id,
            customer.FirstName,
            customer.LastName,
            customer.FullName,
            customer.Phone,
            customer.Document?.Type.ToString(),
            customer.Document?.Value,
            customer.Address?.City,
            customer.Address?.State,
            IsActive: true);
}
