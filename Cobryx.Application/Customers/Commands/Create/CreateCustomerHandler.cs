using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
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
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
        {
            return Result.Failure<Guid>("Tenant context is missing.");
        }

        var existing = await _customerRepository.GetByPhoneAsync(tenantId.Value, request.Phone);
        if (existing != null)
        {
            return Result.Failure<Guid>("Customer with this phone already exists.");
        }

        var customer = new Customer(
            tenantId.Value, // Overriding request.TenantId for security
            request.FullName,
            request.Phone,
            request.Address,
            request.ExternalReference);

        await _customerRepository.AddAsync(customer);
        
        // Note: Real audit log would be handled via interceptors or decorator later
        
        return Result.Success(customer.Id);
    }
}
