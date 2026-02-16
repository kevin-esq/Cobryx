using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.ValueObjects;
using Concordia;

namespace Cobryx.Application.Tenants.Commands.UpdateBusinessSettings;

public record UpdateBusinessSettingsCommand(
    InterestType InterestType,
    decimal DefaultInterestValue,
    PenaltyType PenaltyType,
    decimal PenaltyValue,
    bool AllowPartialPayments,
    PaymentPriority PaymentPriority,
    int GraceDays,
    decimal MinimumPaymentAmount) : IRequest<Result>;

public class UpdateBusinessSettingsHandler : IRequestHandler<UpdateBusinessSettingsCommand, Result>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBusinessSettingsHandler(
        ITenantRepository tenantRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateBusinessSettingsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant == null) return Result.Failure(DomainErrorCode.Tenant.NotFound);

        var newSettings = new BusinessSettings(
            request.InterestType,
            request.DefaultInterestValue,
            request.PenaltyType,
            request.PenaltyValue,
            request.AllowPartialPayments,
            request.PaymentPriority,
            request.GraceDays,
            request.MinimumPaymentAmount);

        tenant.UpdateSettings(newSettings);

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
