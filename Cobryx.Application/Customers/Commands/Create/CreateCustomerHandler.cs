using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Exceptions.Customers;
using Cobryx.Domain.Exceptions.Tenants;
using Concordia;

namespace Cobryx.Application.Customers.Commands.Create;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITenantProvider _tenantProvider;

    public CreateCustomerHandler(ICustomerRepository customerRepository, ITenantProvider tenantProvider)
    {
        _customerRepository = customerRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? request.TenantId;
        if (tenantId == Guid.Empty) throw new TenantContextMissingException();

        var existing = await _customerRepository.GetByPhoneAsync(tenantId, request.Phone);
        if (existing != null)
        {
            throw new DuplicateCustomerException("Customer with this phone number already exists.");
        }

        var customer = new Customer(
            tenantId,
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Address,
            request.Document
        );

        await _customerRepository.AddAsync(customer);

        return Result.Success(customer.Id);
    }
}
