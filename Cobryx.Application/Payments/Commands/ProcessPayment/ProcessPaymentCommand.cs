using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Services;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

namespace Cobryx.Application.Payments.Commands.ProcessPayment;

public record ProcessPaymentCommand(
    Guid CustomerId,
    Guid PaymentMethodId,
    decimal Amount,
    string Currency,
    DateTime PaymentDate,
    string? Reference = null,
    string? Notes = null,
    List<Guid>? InvoiceIds = null) : IRequest<Result<Guid>>;

public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, Result<Guid>>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly PaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessPaymentHandler(
        IPaymentRepository paymentRepository,
        IInvoiceRepository invoiceRepository,
        ITenantProvider tenantProvider,
        PaymentService paymentService,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _tenantProvider = tenantProvider;
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);

        var money = new Money(request.Amount, request.Currency);
        var payment = new Payment(
            tenantId.Value,
            request.CustomerId,
            request.PaymentMethodId,
            money,
            request.PaymentDate,
            request.Reference,
            request.Notes);

        if (request.InvoiceIds != null && request.InvoiceIds.Count != 0)
        {
            decimal remainingAmount = request.Amount;

            foreach (var invoiceId in request.InvoiceIds)
            {
                if (remainingAmount <= 0)
                    break;

                var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
                if (invoice == null || invoice.TenantId != tenantId.Value)
                    continue;

                if (invoice.Status == InvoiceStatus.Paid)
                    continue;

                decimal unpaidAmount = invoice.Total.Amount - invoice.TotalPaid.Amount;
                decimal amountToApply = Math.Min(unpaidAmount, remainingAmount);

                if (amountToApply > 0)
                {
                    payment.AddAllocation(invoice.Id, new Money(amountToApply, request.Currency));
                    remainingAmount -= amountToApply;
                }
            }
        }

        await _paymentRepository.AddAsync(payment, cancellationToken);

        payment.Initiate();
        payment.Complete();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(payment.Id);
    }
}
