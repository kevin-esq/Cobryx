using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Enums;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Credits.Queries.GetCreditById;

public record GetCreditByIdQuery(Guid Id) : IRequest<Result<CreditDetailDto>>;

public class GetCreditByIdHandler : IRequestHandler<GetCreditByIdQuery, Result<CreditDetailDto>>
{
    private readonly ICreditRepository _creditRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetCreditByIdHandler(ICreditRepository creditRepository, ITenantProvider tenantProvider)
    {
        _creditRepository = creditRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<CreditDetailDto>> Handle(GetCreditByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<CreditDetailDto>("Tenant context missing.");

        var credit = await _creditRepository.Query()
            .AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Installments)
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == tenantId.Value, cancellationToken);

        if (credit == null)
        {
            return Result.Failure<CreditDetailDto>("Credit not found.");
        }

        var dto = new CreditDetailDto(
            credit.Id,
            credit.CustomerId,
            $"{credit.Customer.FirstName} {credit.Customer.LastName}",
            credit.Principal.Amount,
            credit.Principal.Currency,
            credit.InterestRate,
            credit.InstallmentsCount,
            credit.Status,
            credit.StartDate,
            credit.Installments.OrderBy(i => i.Number).Select(i => new InstallmentDto(
                i.Number,
                i.DueDate,
                i.TotalDue.Amount,
                i.InterestPart.Amount,
                i.PrincipalPart.Amount,
                i.PrincipalPaid.Amount + i.InterestPaid.Amount + i.LateInterestPaid.Amount,
                i.Status)),
            credit.Payments.OrderByDescending(p => p.PaymentDate).Select(p => new CreditPaymentDto(
                p.Id,
                p.Amount.Amount,
                p.Amount.Currency,
                p.PaymentDate,
                p.Reference)));

        return Result.Success(dto);
    }
}
