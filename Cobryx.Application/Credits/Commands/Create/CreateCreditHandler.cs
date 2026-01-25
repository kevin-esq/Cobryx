using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.ValueObjects;
using Concordia;

namespace Cobryx.Application.Credits.Commands.Create;

public class CreateCreditHandler : IRequestHandler<CreateCreditCommand, Result<Guid>>
{
    private readonly ICreditRepository _creditRepository;
    private readonly IScheduleGenerator _scheduleGenerator;

    public CreateCreditHandler(ICreditRepository creditRepository, IScheduleGenerator scheduleGenerator)
    {
        _creditRepository = creditRepository;
        _scheduleGenerator = scheduleGenerator;
    }

    public async Task<Result<Guid>> Handle(CreateCreditCommand request, CancellationToken cancellationToken)
    {
        var principal = new Money(request.Amount, request.Currency);
        
        var credit = new Credit(
            request.TenantId,
            request.CustomerId,
            principal,
            request.InterestRate,
            request.InterestType,
            request.Frequency,
            request.InstallmentsCount,
            request.GraceDays,
            request.ProductId);

        // Generate installments
        var schedule = _scheduleGenerator.GenerateSchedule(credit);
        credit.AddInstallments(schedule);

        await _creditRepository.AddAsync(credit);

        return Result.Success(credit.Id);
    }
}
