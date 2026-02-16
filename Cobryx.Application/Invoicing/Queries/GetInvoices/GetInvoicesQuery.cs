using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Invoicing;

namespace Cobryx.Application.Invoicing.Queries.GetInvoices;

public record GetInvoicesQuery() : IRequest<Result<IReadOnlyList<InvoiceDto>>>;

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    DateTime IssueDate,
    DateTime DueDate,
    decimal TotalAmount,
    string Currency,
    string Status);

public class GetInvoicesHandler : IRequestHandler<GetInvoicesQuery, Result<IReadOnlyList<InvoiceDto>>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetInvoicesHandler(
        IInvoiceRepository invoiceRepository,
        ITenantProvider tenantProvider)
    {
        _invoiceRepository = invoiceRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<IReadOnlyList<InvoiceDto>>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<IReadOnlyList<InvoiceDto>>("Tenant context missing.");

        var invoices = await _invoiceRepository.GetAllAsync();

        var dtos = invoices
            .Where(i => i.TenantId == tenantId.Value)
            .Select(i => new InvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.CustomerId,
                i.Customer?.FullName ?? CobryxDefaults.UnknownValue,
                i.IssueDate,
                i.DueDate,
                i.Total.Amount,
                i.Total.Currency,
                i.Status.ToString()
            )).ToList();

        return Result.Success<IReadOnlyList<InvoiceDto>>(dtos);
    }
}
