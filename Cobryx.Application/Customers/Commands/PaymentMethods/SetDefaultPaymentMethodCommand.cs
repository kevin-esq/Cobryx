using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Customers.Commands.PaymentMethods;

[TenantScoped]
public record SetDefaultPaymentMethodCommand(Guid CustomerId, string PaymentMethodId) : IRequest<Result>, IRequiresTenant;

public class SetDefaultPaymentMethodHandler : IRequestHandler<SetDefaultPaymentMethodCommand, Result>
{
    private readonly ICobryxDbContext _dbContext;

    public SetDefaultPaymentMethodHandler(ICobryxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(SetDefaultPaymentMethodCommand request, CancellationToken ct)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);

        if (customer == null)
            return Result.Failure(DomainErrorCode.Customer.NotFound);

        customer.SetDefaultPaymentMethod(request.PaymentMethodId);

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
