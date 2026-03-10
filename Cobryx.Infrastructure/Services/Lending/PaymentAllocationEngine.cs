using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Models;
using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Exceptions;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Lending.Enums;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Services.Lending;

public class PaymentAllocationEngine : IPaymentAllocationEngine
{
    private readonly ILogger<PaymentAllocationEngine> _logger;

    public PaymentAllocationEngine(ILogger<PaymentAllocationEngine> logger)
    {
        _logger = logger;
    }

    public Task<LoanPaymentAllocation> AllocateAsync(Loan loan, decimal paymentAmount, Guid paymentId, CancellationToken ct = default)
    {
        if (paymentAmount <= 0)
            throw new DomainException(DomainErrorCode.Loans.InvalidPaymentAmount);

        _logger.LogInformation("Allocating payment of {Amount} for Loan {LoanId}", paymentAmount, loan.Id);

        // adjust precision if mapping from external source, but here we assume decimals are passed correctly
        var remaining = paymentAmount;

        var principalApplied = 0m;
        var interestApplied = 0m;
        var feesApplied = 0m;

        var policy = loan.Agreement.PaymentApplicationPolicy;
        if (policy == null)
            throw new DomainException(DomainErrorCode.Loans.InvalidLateFeePolicy);

        var priorityOrder = policy.GetPriorityOrder();

        foreach (var category in priorityOrder)
        {
            if (remaining <= 0) break;

            decimal outstanding = category switch
            {
                PaymentApplicationType.LateFees => loan.OutstandingFees,
                PaymentApplicationType.Interest => loan.OutstandingInterest,
                PaymentApplicationType.Principal => loan.OutstandingPrincipal,
                _ => 0m
            };

            var applied = Math.Min(outstanding, remaining);

            switch (category)
            {
                case PaymentApplicationType.LateFees:
                    feesApplied = applied;
                    break;
                case PaymentApplicationType.Interest:
                    interestApplied = applied;
                    break;
                case PaymentApplicationType.Principal:
                    principalApplied = applied;
                    break;
            }

            remaining -= applied;
        }

        var allocation = new LoanPaymentAllocation(
            loan.TenantId,
            loan.Id,
            paymentId,
            principalApplied,
            interestApplied,
            feesApplied,
            remaining, // UnappliedAmount
            0 // Initialized at 0; updated by posting engine if applicable.
        );

        return Task.FromResult(allocation);
    }
}
