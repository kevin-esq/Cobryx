using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Invoicing.Commands.CreateInvoice;

public record CreateInvoiceCommand(
    Guid CustomerId,
    DateTime DueDate,
    List<InvoiceItemRequest> Items,
    string? Notes = null) : IRequest<Result<Guid>>;

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
    ISubscriptionEnforcementService subscriptionEnforcement) : IRequestHandler<CreateInvoiceCommand, Result<Guid>>
{
    private readonly IInvoiceRepository _invoiceRepository = invoiceRepository;
    private readonly ITaxConfigurationRepository _taxRepository = taxRepository;
    private readonly ITenantProvider _tenantProvider = tenantProvider;
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly IInvoiceNumberService _invoiceNumberService = invoiceNumberService;
    private readonly ISubscriptionEnforcementService _subscriptionEnforcement = subscriptionEnforcement;

    public async Task<Result<Guid>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);

        await _subscriptionEnforcement.EnsureWithinInvoicesLimitAsync(tenantId.Value, cancellationToken);

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null || customer.TenantId != tenantId.Value)
            return Result.Failure<Guid>(DomainErrorCode.Customer.NotFound);

        var defaultTax = await _taxRepository.GetDefaultAsync(tenantId.Value, cancellationToken);
        var invoiceNumber = await _invoiceNumberService.GenerateNextNumberAsync(tenantId.Value);

        var invoice = new Invoice(
            tenantId.Value,
            request.CustomerId,
            invoiceNumber,
            DateTime.UtcNow,
            request.DueDate);

        foreach (var itemReq in request.Items)
        {
            var taxConfig = itemReq.TaxConfigurationId.HasValue
                ? await _taxRepository.GetByIdAsync(itemReq.TaxConfigurationId.Value, cancellationToken)
                : defaultTax;

            decimal rate = taxConfig?.Rate ?? 0;
            bool inclusive = taxConfig?.IsInclusive ?? false;

            invoice.AddItem(itemReq.Description, itemReq.Quantity, itemReq.UnitPrice, rate, inclusive);
        }

        await _invoiceRepository.AddAsync(invoice, cancellationToken);

        return Result.Success(invoice.Id);
    }
}
