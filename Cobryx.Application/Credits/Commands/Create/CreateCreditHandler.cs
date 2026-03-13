using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

namespace Cobryx.Application.Credits.Commands.Create;

public class CreateCreditHandler(
    ICreditRepository creditRepository,
    IScheduleGenerator scheduleGenerator,
    ITenantProvider tenantProvider) : IRequestHandler<CreateCreditCommand, Result<Guid>>
{
    private readonly ICreditRepository _creditRepository = creditRepository;
    private readonly IScheduleGenerator _scheduleGenerator = scheduleGenerator;
    private readonly ITenantProvider _tenantProvider = tenantProvider;

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

        await _creditRepository.AddAsync(credit, cancellationToken);

        return Result.Success(credit.Id);
    }
}
