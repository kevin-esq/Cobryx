using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Payments.Commands.RetryPayment;

[TenantScoped]
public record RetryPaymentCommand(
    Guid PaymentId,
    int AttemptNumber) : IRequest<Result>, IRequiresTenant, IFinancialCommand;

public class RetryPaymentHandler(
    IPaymentRepository paymentRepository,
    ITenantProvider tenantProvider,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RetryPaymentCommand, Result>
{
    public async Task<Result> Handle(RetryPaymentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        var payment = await paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment == null)
            return Result.Failure(DomainErrorCode.Invoicing.PaymentNotFound);

        if (payment.TenantId != tenantId.Value)
            return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        try
        {
            // Set for Retry processing
            payment.Retry(request.AttemptNumber);

            // Hardening: Capturing Resource Metadata for Introspection
            // This is critical for SRE to know which attempt they are looking at in the logs.
            var idemContext = idempotencyStore.GetCurrentContext();
            if (idemContext != null)
            {
                await idempotencyStore.CompleteWithinTransactionAsync(
                    tenantId.Value,
                    idemContext.IdempotencyKey,
                    202, // Accepted
                    null,
                    "application/json",
                    resourceType: "payment",
                    resourceId: payment.Id,
                    environment: "production",
                    correlationId: Guid.Empty);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Note: In a real system, this would trigger the actual Gateway Retry loop.
            // For now, it updates the state deterministically.

            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.ErrorCode);
        }
    }
}
