using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.ValueObjects;
using Concordia;

namespace Cobryx.Application.Payments.Commands.Register;

public class RegisterPaymentHandler : IRequestHandler<RegisterPaymentCommand, Result<Guid>>
{
    private readonly ICreditRepository _creditRepository;
    private readonly IPaymentRepository _paymentRepository;

    public RegisterPaymentHandler(ICreditRepository creditRepository, IPaymentRepository paymentRepository)
    {
        _creditRepository = creditRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<Guid>> Handle(RegisterPaymentCommand request, CancellationToken cancellationToken)
    {
        var credit = await _creditRepository.GetByIdAsync(request.CreditId);
        if (credit == null || credit.TenantId != request.TenantId)
        {
            return Result.Failure<Guid>("Credit not found.");
        }

        var amount = new Money(request.Amount, request.Currency);
        
        // Record the payment entry (Generate ID first)
        var payment = new Payment(
            request.TenantId,
            request.CreditId,
            amount,
            request.PaymentDate,
            request.Reference,
            request.Notes);

        // Apply logic to domain (State change) - Passing the payment ID for event decoupling
        credit.ApplyPayment(payment.Id, amount);

        await _paymentRepository.AddAsync(payment);
        await _creditRepository.UpdateAsync(credit);

        return Result.Success(payment.Id);
    }
}
