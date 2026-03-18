using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Exceptions.Customers;
using Cobryx.Domain.Exceptions.Tenants;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

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
        if (!tenantId.HasValue)
            throw new TenantContextMissingException();

        var customer = await _customerRepository.GetByIdAsync(request.Id, cancellationToken);

        if (customer == null || customer.TenantId != tenantId.Value)
        {
            throw new CustomerNotFoundException(request.Id);
        }

        await _customerRepository.DeleteAsync(customer.Id, cancellationToken);

        return Result.Success();
    }
}
