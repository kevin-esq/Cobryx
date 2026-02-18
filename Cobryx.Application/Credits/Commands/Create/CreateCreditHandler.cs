using Cobryx.Application.Common.Interfaces;
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
    private readonly ITenantProvider _tenantProvider;

    public CreateCreditHandler(
        ICreditRepository creditRepository,
        IScheduleGenerator scheduleGenerator,
        ITenantProvider tenantProvider)
    {
        _creditRepository = creditRepository;
        _scheduleGenerator = scheduleGenerator;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<Guid>> Handle(CreateCreditCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
        {
            return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);
        }

        var principal = new Money(request.Amount, request.Currency);

        var credit = new Credit(
            tenantId.Value,
            request.CustomerId,
            principal,
            request.InterestRate,
            request.InterestType,
            request.Frequency,
            request.InstallmentsCount,
            request.GraceDays,
            request.ProductId);


        var schedule = _scheduleGenerator.GenerateSchedule(credit);
        credit.AddInstallments(schedule);

        await _creditRepository.AddAsync(credit);

        return Result.Success(credit.Id);
    }
}
