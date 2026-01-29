using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Customers.Commands.Delete;

public record DeleteCustomerCommand(Guid Id) : IRequest<Result>;

public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, Result>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITenantProvider _tenantProvider;

    public DeleteCustomerHandler(ICustomerRepository customerRepository, ITenantProvider tenantProvider)
    {
        _customerRepository = customerRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure("Tenant context missing.");

        var customer = await _customerRepository.GetByIdAsync(request.Id);

        if (customer == null || customer.TenantId != tenantId.Value)
        {
            return Result.Failure("Customer not found.");
        }


        await _customerRepository.DeleteAsync(customer.Id);

        return Result.Success();
    }
}
