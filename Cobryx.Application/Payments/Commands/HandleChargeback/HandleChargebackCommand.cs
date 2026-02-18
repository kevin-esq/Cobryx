using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Payments.Commands.HandleChargeback;

public record HandleChargebackCommand(Guid PaymentId) : IRequest<Result>;

public class HandleChargebackHandler : IRequestHandler<HandleChargebackCommand, Result>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public HandleChargebackHandler(IPaymentRepository paymentRepository, IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(HandleChargebackCommand request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);
        if (payment == null) return Result.Failure(DomainErrorCode.Invoicing.PaymentNotFound);

        try
        {
            payment.Chargeback();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.ErrorCode);
        }
    }
}
