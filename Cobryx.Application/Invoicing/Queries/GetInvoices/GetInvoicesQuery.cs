using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Invoicing.Queries.GetInvoices
{
    [TenantScoped]
    public record GetInvoicesQuery : IRequest<Result<IReadOnlyList<InvoiceDto>>>, IRequiresTenant;

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

    public class GetInvoicesHandler(
        IInvoiceRepository invoiceRepository,
        ITenantProvider tenantProvider) : IRequestHandler<GetInvoicesQuery, Result<IReadOnlyList<InvoiceDto>>>
    {
        public async Task<Result<IReadOnlyList<InvoiceDto>>> Handle(GetInvoicesQuery request,
            CancellationToken cancellationToken)
        {
            Guid? tenantId = tenantProvider.GetTenantId();
            if (!tenantId.HasValue)
            {
                return Result.Failure<IReadOnlyList<InvoiceDto>>(DomainErrorCode.Tenant.ContextMissing);
            }

            IEnumerable<Invoice> invoices = await invoiceRepository.GetAllAsync(cancellationToken);

            var dtos = invoices
                .Where(i => i.TenantId == tenantId.Value)
                .Select(i => new InvoiceDto(
                    i.Id,
                    i.InvoiceNumber,
                    i.CustomerId,
                    i.Customer.FullName,
                    i.IssueDate,
                    i.DueDate,
                    i.Total.Amount,
                    i.Total.Currency,
                    i.Status.ToString()
                )).ToList();

            return Result.Success<IReadOnlyList<InvoiceDto>>(dtos);
        }
    }
}
