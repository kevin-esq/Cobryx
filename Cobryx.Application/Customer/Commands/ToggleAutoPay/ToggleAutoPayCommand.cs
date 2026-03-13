using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Customer.Commands.ToggleAutoPay;

public record ToggleAutoPayCommand(Guid CustomerId, bool Enabled) : IRequest<Result>;

public class ToggleAutoPayHandler : IRequestHandler<ToggleAutoPayCommand, Result>
{
    private readonly ICobryxDbContext _dbContext;

    public ToggleAutoPayHandler(ICobryxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(ToggleAutoPayCommand request, CancellationToken ct)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);

        if (customer == null)
            return Result.Failure(DomainErrorCode.Customer.NotFound);

        customer.ToggleAutoPay(request.Enabled);

        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
