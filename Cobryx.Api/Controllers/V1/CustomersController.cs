using Asp.Versioning;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Contracts.V1.Customers;
using Cobryx.Api.Outcomes;
using Cobryx.Application.Common.Interfaces;

using Cobryx.Domain.ValueObjects;
using Cobryx.Application.Common.Models;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for managing the lifecycle of customer records, including profiles, identity validation, and contact information.
/// </summary>
[Authorize(Policy = "CanCreateCustomers")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers")]
[Tags("Customers")]
public class CustomersController : CobryxBaseController
{
    private readonly ITenantProvider _tenantProvider;

    public CustomersController(ISender sender, ITenantProvider tenantProvider) : base(sender)
    {
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Registers a new customer and establishes their record within the tenant context.
    /// </summary>
    /// <param name="request">Comprehensive customer data including identity documents and address details.</param>
    /// <remarks>
    /// All address fields are validated against geographical standards. Identity documents must follow the specified type patterns.
    ///
    /// Possible Outcomes:
    /// - CUSTOMER.CREATED: Customer successfully registered.
    /// - CUSTOMER.FAILED: Validation failed (e.g., existing document number or invalid phone).
    /// </remarks>
    /// <response code="201">Returns the identifier for the registered customer.</response>
    /// <response code="400">Invalid parameters or malformed data.</response>
    /// <response code="409">Conflict (e.g., document number already exists for this tenant).</response>
    [HttpPost]
    [Authorize(Policy = "CanCreateCustomers")]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 409)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        // Intentional Mapping: Public Request -> Internal Domain Value Objects
        var address = request.Address != null
            ? new Address(
                request.Address.Street,
                request.Address.HouseNumber,
                request.Address.ApartmentNumber,
                request.Address.Neighborhood ?? string.Empty,
                request.Address.PostalCode ?? string.Empty,
                request.Address.City ?? string.Empty,
                request.Address.State ?? string.Empty)
            : null;

        var document = request.Document != null
            ? new IdentityDocument(
                request.Document.Type,
                request.Document.Number)
            : null;

        var command = new Application.Customers.Commands.Create.CreateCustomerCommand(
            tenantId.Value,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            address,
            document);

        var result = await Sender.Send(command);
        return HandleCreatedResult($"/api/v1/customers/{result.Value}", result, CustomerOutcomes.Created);
    }

    /// <summary>
    /// Retrieves a paginated list of customers, optionally filtered by a search term.
    /// </summary>
    /// <param name="searchTerm">Partial match for name, email, or document number.</param>
    /// <param name="page">Pagination index (1-based).</param>
    /// <param name="pageSize">Records per page result set.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - CUSTOMER.SEARCH.COMPLETED: Search completed successfully.
    /// </remarks>
    /// <response code="200">A paginated list of customers.</response>
    [HttpGet]
    [Authorize(Policy = "CanViewCustomers")]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<PaginatedList<CustomerSummaryContract>>), 200)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 403)]
    public async Task<IActionResult> GetAll([FromQuery] string? searchTerm, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await Sender.Send(new Application.Customers.Queries.GetCustomers.GetCustomersQuery(searchTerm, page, pageSize));

        if (!result.IsSuccess || result.Value == null)
        {
            return HandleResult(result, CustomerOutcomes.SearchCompleted);
        }

        var mapped = new PaginatedList<CustomerSummaryContract>(
            result.Value.Items.Select(c => new CustomerSummaryContract(
                c.Id,
                c.FirstName,
                c.LastName,
                c.FullName,
                c.Phone,
                c.Document?.Type.ToString(),
                c.Document?.Value,
                c.Address?.City,
                c.Address?.State,
                true)).ToList(), // Assuming active for now as soft-delete logic is being finalized
            result.Value.TotalCount,
            result.Value.Page,
            result.Value.TotalPages);

        return Success(mapped, CustomerOutcomes.SearchCompleted);
    }

    /// <summary>
    /// Retrieves complete profile details for a specific customer.
    /// </summary>
    /// <param name="id">Unique identifier for the customer resource.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - CUSTOMER.SEARCH.COMPLETED: Customer profile retrieved.
    /// - CUSTOMER.FAILED: Customer record not found.
    /// </remarks>
    /// <response code="200">The requested customer profile.</response>
    /// <response code="404">No customer found with the provided ID.</response>
    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewCustomers")]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<CustomerSummaryContract>), 200)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Sender.Send(new Application.Customers.Queries.GetCustomerById.GetCustomerByIdQuery(id));

        if (!result.IsSuccess || result.Value == null)
        {
            return HandleResult(result, CustomerOutcomes.SearchCompleted);
        }

        var mapped = new CustomerSummaryContract(
            result.Value.Id,
            result.Value.FirstName,
            result.Value.LastName,
            result.Value.FullName,
            result.Value.Phone,
            result.Value.Document?.Type.ToString(),
            result.Value.Document?.Value,
            result.Value.Address?.City,
            result.Value.Address?.State,
            true);

        return Success(mapped, CustomerOutcomes.SearchCompleted);
    }

    /// <summary>
    /// Updates an existing customer's contact or profile details.
    /// </summary>
    /// <param name="id">Unique identifier of the customer to update.</param>
    /// <param name="request">Partial or full customer details for update.</param>
    /// <remarks>
    /// Only provided fields will be modified.
    ///
    /// Possible Outcomes:
    /// - CUSTOMER.UPDATED: Changes successfully persisted.
    /// - CUSTOMER.FAILED: Validation failed or customer record missing.
    /// </remarks>
    /// <response code="200">Success envelope indicating completion.</response>
    /// <response code="400">Invalid data provided.</response>
    /// <response code="404">Customer not found.</response>
    [HttpPut("{id}")]
    [Authorize(Policy = "CanCreateCustomers")]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        // Intentional Mapping: Public Request -> Internal Domain Value Objects
        var address = request.Address != null
            ? new Address(
                request.Address.Street,
                request.Address.HouseNumber,
                request.Address.ApartmentNumber,
                request.Address.Neighborhood ?? string.Empty,
                request.Address.PostalCode ?? string.Empty,
                request.Address.City ?? string.Empty,
                request.Address.State ?? string.Empty)
            : null;

        var document = request.Document != null
            ? new IdentityDocument(
                request.Document.Type,
                request.Document.Number)
            : null;

        var command = new Application.Customers.Commands.Update.UpdateCustomerCommand(
            id,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            address,
            document);

        var result = await Sender.Send(command);
        return HandleResult(result, CustomerOutcomes.Updated);
    }

    /// <summary>
    /// Formally deactivates and removes a customer record from active operations (Soft Delete).
    /// </summary>
    /// <param name="id">Identifier of the customer to deactivate.</param>
    /// <remarks>
    /// Historical data (invoices, loans) remains associated with the record for audit purposes.
    ///
    /// Possible Outcomes:
    /// - CUSTOMER.DELETED: Record successfully deactivated.
    /// - CUSTOMER.FAILED: Customer not found.
    /// </remarks>
    /// <response code="204">Customer successfully deactivated.</response>
    /// <response code="404">Customer not found.</response>
    [HttpDelete("{id}")]
    [Authorize(Policy = "CanCreateCustomers")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(Cobryx.Api.Contracts.V1.Common.ApiErrorResponse), 404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await Sender.Send(new Application.Customers.Commands.Delete.DeleteCustomerCommand(id));
        return HandleDeleteResult(result, CustomerOutcomes.Deleted);
    }
}
