using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Exceptions;
using Cobryx.Domain.Common;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Tenants.Commands.PurgeDemoData;

public record PurgeDemoDataCommand : IRequest<Result>;

public class PurgeDemoDataHandler : IRequestHandler<PurgeDemoDataCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public PurgeDemoDataHandler(IUnitOfWork unitOfWork, ITenantProvider tenantProvider)
    {
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result> Handle(PurgeDemoDataCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? throw new DomainException(DomainErrorCode.Tenant.ContextMissing);
        var dbContext = (DbContext)_unitOfWork;

        var demoLoans = await dbContext.Set<Loan>()
            .Where(l => l.TenantId == tenantId && l.IsDemo)
            .ToListAsync(cancellationToken);
        
        if (demoLoans.Any())
        {
            dbContext.Set<Loan>().RemoveRange(demoLoans);
        }

        var demoPayments = await dbContext.Set<Payment>()
            .Where(p => p.TenantId == tenantId && p.IsDemo)
            .ToListAsync(cancellationToken);
        
        if (demoPayments.Any())
        {
            dbContext.Set<Payment>().RemoveRange(demoPayments);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
