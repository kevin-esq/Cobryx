using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Payments.Commands.ProcessPayment;

[TenantScoped]
public record ProcessPaymentCommand(
    Guid CustomerId,
    Guid PaymentMethodId,
    decimal Amount,
    string Currency,
    DateTime PaymentDate,
    string? Reference = null,
    string? Notes = null,
    List<Guid>? InvoiceIds = null) : IRequest<Result<Guid>>, IRequiresTenant;

public partial class ProcessPaymentHandler(
    IPaymentRepository paymentRepository,
    IInvoiceRepository invoiceRepository,
    ITenantProvider tenantProvider,
    IUnitOfWork unitOfWork,
    ILogger<ProcessPaymentHandler> logger) : IRequestHandler<ProcessPaymentCommand, Result<Guid>>
{
    private const int MaxConcurrencyRetries = 3;

    public async Task<Result<Guid>> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantProvider.GetTenantId();
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
            var allocations = await AllocateToInvoicesWithRetryAsync(
                tenantId.Value, payment, request.InvoiceIds, request.Amount, request.Currency, cancellationToken);

            if (allocations.IsFailure)
                return Result.Failure<Guid>(allocations.Error!);
        }

        await paymentRepository.AddAsync(payment, cancellationToken);

        payment.Initiate();
        payment.Complete();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(payment.Id);
    }

    /// <summary>
    /// Allocates payment to invoices with optimistic concurrency retry.
    /// Prevents race conditions where two payments try to pay the same invoice.
    /// </summary>
    private async Task<Result> AllocateToInvoicesWithRetryAsync(
        Guid tenantId,
        Payment payment,
        List<Guid> invoiceIds,
        decimal totalAmount,
        string currency,
        CancellationToken ct)
    {
        for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            try
            {
                decimal remainingAmount = totalAmount;

                foreach (var invoiceId in invoiceIds)
                {
                    if (remainingAmount <= 0)
                        break;

                    var invoice = await invoiceRepository.GetByIdAsync(invoiceId, ct);
                    if (invoice == null || invoice.TenantId != tenantId)
                        continue;

                    if (invoice.Status == InvoiceStatus.Paid)
                        continue;

                    // Calculate amount based on current state (may have changed since last read)
                    decimal unpaidAmount = invoice.Total.Amount - invoice.TotalPaid.Amount;
                    decimal amountToApply = Math.Min(unpaidAmount, remainingAmount);

                    if (amountToApply > 0)
                    {
                        payment.AddAllocation(invoice.Id, new Money(amountToApply, currency));

                        // Increment version for optimistic concurrency check
                        invoice.IncrementVersion();

                        remainingAmount -= amountToApply;
                    }
                }

                return Result.Success();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                LogConcurrencyConflict(logger, attempt + 1, ex);

                if (attempt == MaxConcurrencyRetries - 1)
                {
                    return Result.Failure(DomainErrorCode.Invoicing.ConcurrencyConflict);
                }

                // Clear allocations and retry with fresh data
                payment.ClearDomainEvents();

                // Small delay before retry
                await Task.Delay(50 * (attempt + 1), ct);
            }
        }

        return Result.Failure(DomainErrorCode.Invoicing.ConcurrencyConflict);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Payment allocation concurrency conflict, attempt {Attempt}")]
    private static partial void LogConcurrencyConflict(ILogger logger, int attempt, Exception ex);
}
