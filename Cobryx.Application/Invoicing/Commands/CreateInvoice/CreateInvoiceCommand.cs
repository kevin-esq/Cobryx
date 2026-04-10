using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Invoicing.Commands.CreateInvoice
{
    [TenantScoped]
    public record CreateInvoiceCommand(
        Guid CustomerId,
        DateTime DueDate,
        List<InvoiceItemRequest> Items,
        string? Notes = null) : IRequest<Result<Guid>>, IRequiresTenant;

    public record InvoiceItemRequest(
        string Description,
        decimal Quantity,
        decimal UnitPrice,
        Guid? TaxConfigurationId = null);

    public class CreateInvoiceHandler(
        IInvoiceRepository invoiceRepository,
        ITaxConfigurationRepository taxRepository,
        ITenantProvider tenantProvider,
        ICustomerRepository customerRepository,
        IInvoiceNumberService invoiceNumberService,
        IClock clock,
        ISubscriptionEnforcementService subscriptionEnforcement) : IRequestHandler<CreateInvoiceCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
        {
            Guid? tenantId = tenantProvider.GetTenantId();
            if (!tenantId.HasValue)
            {
                return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);
            }

            await subscriptionEnforcement.EnsureWithinInvoicesLimitAsync(tenantId.Value, cancellationToken);

            Domain.Lending.Customer? customer =
                await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null || customer.TenantId != tenantId.Value)
            {
                return Result.Failure<Guid>(DomainErrorCode.Customer.NotFound);
            }

            TaxConfiguration? defaultTax = await taxRepository.GetDefaultAsync(tenantId.Value, cancellationToken);
            var invoiceNumber = await invoiceNumberService.GenerateNextNumberAsync(tenantId.Value);

            var invoice = new Invoice(
                tenantId.Value,
                request.CustomerId,
                invoiceNumber,
                clock.UtcNow,
                request.DueDate);

            foreach (InvoiceItemRequest itemReq in request.Items)
            {
                TaxConfiguration? taxConfig = itemReq.TaxConfigurationId.HasValue
                    ? await taxRepository.GetByIdAsync(itemReq.TaxConfigurationId.Value, cancellationToken)
                    : defaultTax;

                var rate = taxConfig?.Rate ?? 0;
                var inclusive = taxConfig?.IsInclusive ?? false;

                invoice.AddItem(itemReq.Description, itemReq.Quantity, itemReq.UnitPrice, rate, inclusive);
            }

            await invoiceRepository.AddAsync(invoice, cancellationToken);

            return Result.Success(invoice.Id);
        }
    }
}
