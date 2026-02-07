using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.ValueObjects;
using Concordia;

namespace Cobryx.Application.Payments.Commands.RefundPayment;

public record RefundPaymentCommand(
    Guid PaymentId,
    decimal Amount,
    string Currency) : IRequest<Result>;

public class RefundPaymentHandler : IRequestHandler<RefundPaymentCommand, Result>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RefundPaymentHandler(IPaymentRepository paymentRepository, IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
        if (payment == null) return Result.Failure("Payment not found.");

        var refundAmount = new Money(request.Amount, request.Currency);
        
        try 
        {
            payment.Refund(refundAmount);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
