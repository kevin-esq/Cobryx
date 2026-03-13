using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Exceptions.Customers;
using Cobryx.Domain.Exceptions.Tenants;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Customers.Commands.Create;

public class CreateCustomerHandler(ICustomerRepository customerRepository, ITenantProvider tenantProvider) : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly ITenantProvider _tenantProvider = tenantProvider;

    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? request.TenantId;
        if (tenantId == Guid.Empty) throw new TenantContextMissingException();

        var existing = await _customerRepository.GetByPhoneAsync(tenantId, request.Phone, cancellationToken);
        if (existing != null)
        {
            throw new DuplicateCustomerException();
        }

        var customer = new Cobryx.Domain.Lending.Customer(
            tenantId,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            request.Address,
            request.Document
        );

        await _customerRepository.AddAsync(customer, cancellationToken);

        return Result.Success(customer.Id);
    }
}
