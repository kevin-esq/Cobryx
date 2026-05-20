using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Tenants.Commands.SeedDemoData;

[TenantScoped]
public record SeedDemoDataCommand : IRequest<Result>, IRequiresTenant;

public class SeedDemoDataHandler : IRequestHandler<SeedDemoDataCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly IClock _clock;

    public SeedDemoDataHandler(IUnitOfWork unitOfWork, ITenantProvider tenantProvider, IClock clock)
    {
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _clock = clock;
    }

    public async Task<Result> Handle(SeedDemoDataCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? throw new DomainException(DomainErrorCode.Tenant.ContextMissing);
        var dbContext = (DbContext)_unitOfWork;

        var alreadyHasDemo = await dbContext.Set<Loan>().AnyAsync(l => l.TenantId == tenantId && l.IsDemo, cancellationToken);
        if (alreadyHasDemo)
            return Result.Success();

        var customer = new Cobryx.Domain.Lending.Customer(
            tenantId,
            "Demo",
            "Global Corp",
            "+525500000000",
            "demo@globalcorp.com",
            null,
            null);

        dbContext.Set<Cobryx.Domain.Lending.Customer>().Add(customer);

        var agreement = new LoanAgreement(
            tenantId,
            customer.Id,
            500000,
            Guid.NewGuid(),
            PaymentFrequency.Monthly,
            12,
            _clock.UtcNow.AddDays(-60),
            _clock.UtcNow.AddDays(-30),
            LoanOrigin.CashLoan);

        dbContext.Set<LoanAgreement>().Add(agreement);

        var activeLoan = new Loan(tenantId, customer.Id, agreement.Id, "DEMO-LN-ACTIVE", new Money(250000, CobryxDefaults.Currency), isDemo: true);
        activeLoan.Activate();

        var overdueLoan = new Loan(tenantId, customer.Id, agreement.Id, "DEMO-LN-OVERDUE", new Money(100000, CobryxDefaults.Currency), isDemo: true);
        overdueLoan.Activate();

        dbContext.Set<Loan>().AddRange(activeLoan, overdueLoan);

        var paymentMethod = await dbContext.Set<PaymentMethod>()
            .FirstOrDefaultAsync(pm => pm.TenantId == tenantId, cancellationToken);

        if (paymentMethod != null)
        {
            var payment = new Payment(
                tenantId,
                customer.Id,
                paymentMethod.Id,
                new Money(15000, CobryxDefaults.Currency),
                _clock.UtcNow.AddDays(-5),
                "DEMO-REF-001",
                "Demo Payment Success",
                isDemo: true);

            payment.Initiate();
            payment.Complete();
            dbContext.Set<Payment>().Add(payment);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
