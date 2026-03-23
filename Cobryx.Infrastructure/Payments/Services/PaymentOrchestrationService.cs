using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Application.Payments.Commands.CreatePaymentLink;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using CustomerEntity = Cobryx.Domain.Lending.Customer;

namespace Cobryx.Infrastructure.Payments.Services;

public class PaymentOrchestrationService : IPaymentOrchestrationService
{
    private readonly ICobryxDbContext _dbContext;
    private readonly IStripeService _stripeService;
    private readonly ISender _sender;
    private readonly CobryxMetrics _metrics;
    private readonly ILogger<PaymentOrchestrationService> _logger;

    public PaymentOrchestrationService(
        ICobryxDbContext dbContext,
        IStripeService stripeService,
        ISender sender,
        CobryxMetrics metrics,
        ILogger<PaymentOrchestrationService> logger)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
        _sender = sender;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<Result> HandlePaymentFailureAsync(
        Guid customerId,
        string? stripeFailureCode,
        decimal amount,
        string currency,
        string description,
        string? stripePaymentIntentId = null,
        Guid? paymentLinkId = null,
        CancellationToken ct = default)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, ct);

        if (customer == null)
            return Result.Failure(DomainErrorCode.Customer.NotFound);

        PaymentLink? link = null;
        if (paymentLinkId.HasValue)
        {
            link = await _dbContext.PaymentLinks.FirstOrDefaultAsync(l => l.Id == paymentLinkId.Value, ct);
        }
        else if (!string.IsNullOrEmpty(stripePaymentIntentId))
        {
            link = await _dbContext.PaymentLinks.FirstOrDefaultAsync(l => l.StripePaymentIntentId == stripePaymentIntentId, ct);
        }

        if (link != null)
        {
            if (!link.TryAcquireRecoveryLock())
            {
                _logger.LogWarning("Execution skipped for PaymentLink {Id}: Already processing or completed.", link.Id);
                return Result.Success();
            }

            if (link.LoanId.HasValue)
            {
                var loan = await _dbContext.Loans.FirstOrDefaultAsync(l => l.Id == link.LoanId.Value, ct);
                if (loan != null)
                {
                    if (loan.Status == LoanStatus.Disputed)
                    {
                        _logger.LogWarning("Recovery aborted for Loan {LoanId}: Active dispute detected.", loan.Id);
                        _metrics.RecordRecoveryDisputeStop(customerId);
                        return Result.Success();
                    }

                    if (loan.Status == LoanStatus.Closed || (loan.CurrentPrincipalBalance + loan.CurrentInterestBalance + loan.CurrentLateFeeBalance) <= 0)
                    {
                        _logger.LogInformation("Recovery skipped for Loan {LoanId}: Already settled or closed.", loan.Id);
                        return Result.Success();
                    }
                }
            }
        }

        try
        {
            var failureType = ClassifyFailure(stripeFailureCode);

            if (failureType == FailureType.SoftDecline && customer.AutoPayEnabled && !string.IsNullOrEmpty(customer.DefaultPaymentMethodId))
            {
                _logger.LogInformation("Attempting automated recovery charge for Customer {CustomerId} due to {Code}", customerId, stripeFailureCode);

                try
                {
                    int currentAttempt = (link?.RecoveryAttemptCount ?? 0) + 1;
                    string? idempotencyKey = link != null ? $"recovery_{link.Id}_{currentAttempt}" : null;

                    var intentId = await _stripeService.ChargeSavedPaymentMethodAsync(
                        customer.StripeCustomerId!,
                        customer.DefaultPaymentMethodId,
                        amount,
                        currency,
                        $"Automated Recovery: {description}",
                        null,
                        idempotencyKey,
                        null,
                        ct);

                    _logger.LogInformation("Recovery charge successful. Intent: {IntentId}", intentId);

                    if (link != null)
                    {
                        link.RecordRecoveryAttempt();
                        _metrics.RecordRecoveryAttempt(currentAttempt, "success");
                        _metrics.RecordRecoveryRevenue((double)amount, currency);
                        await _dbContext.SaveChangesAsync(ct);
                    }

                    return Result.Success();
                }
                catch (global::Stripe.StripeException ex) when (ex.StripeError?.Type == "card_error" && ex.StripeError?.Code == "authentication_required")
                {
                    _logger.LogWarning("Recovery charge for Customer {CustomerId} requires 3DS. Escalating.", customerId);
                    _metrics.RecordRecoveryAttempt((link?.RecoveryAttemptCount ?? 0) + 1, "requires_auth", "3ds_required");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Automated recovery charge failed for Customer {CustomerId}.", customerId);

                    if (link != null)
                    {
                        link.RecordRecoveryFailure(stripeFailureCode ?? "unknown_error");
                        _metrics.RecordRecoveryAttempt(link.RecoveryAttemptCount, "failure", stripeFailureCode);
                        await _dbContext.SaveChangesAsync(ct);
                    }

                    return Result.Success();
                }
            }

            return await GenerateRetryLinkAsync(customer, amount, currency, description, ct);
        }
        finally
        {
            if (link != null)
            {
                link.ReleaseRecoveryLock();
                await _dbContext.SaveChangesAsync(ct);
            }
        }
    }

    private static FailureType ClassifyFailure(string? code)
    {
        return code switch
        {
            "insufficient_funds" => FailureType.SoftDecline,
            "try_again_later" => FailureType.SoftDecline,
            "issuer_unavailable" => FailureType.SoftDecline,
            "processing_error" => FailureType.SoftDecline,
            "card_velocity_exceeded" => FailureType.SoftDecline,
            "temporary_hold" => FailureType.SoftDecline,

            "expired_card" => FailureType.HardDecline,
            "invalid_card_number" => FailureType.HardDecline,
            "stolen_card" => FailureType.HardDecline,
            "lost_card" => FailureType.HardDecline,
            "do_not_honor" => FailureType.HardDecline,
            "authentication_required" => FailureType.HardDecline,

            _ => FailureType.SoftDecline
        };
    }

    private async Task<Result> GenerateRetryLinkAsync(
        CustomerEntity customer,
        decimal amount,
        string currency,
        string description,
        CancellationToken ct)
    {
        _ = description;
        var command = new CreatePaymentLinkCommand(
            customer.Id,
            new Money(amount, currency),
            null,
            $"RECOVERY-{Guid.NewGuid():N}",
            3
        );

        var result = await _sender.Send(command, ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Recovery link generated for Customer {CustomerId}: {Token}", customer.Id, result.Value);
            return Result.Success();
        }

        return Result.Failure(result.Error ?? DomainErrorCode.Common.GeneralError);
    }

    private enum FailureType
    {
        SoftDecline,
        HardDecline
    }
}
