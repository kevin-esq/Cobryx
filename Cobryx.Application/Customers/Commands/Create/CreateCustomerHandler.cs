using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Customers.Commands.Create;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    private readonly ICustomerRepository _repository;

    public CreateCustomerHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByPhoneAsync(request.TenantId, request.Phone);
        if (existing != null)
        {
            return Result.Failure<Guid>("Customer with this phone already exists.");
        }

        var customer = new Customer(
            request.TenantId,
            request.FullName,
            request.Phone,
            request.Address,
            request.ExternalReference);

        await _repository.AddAsync(customer);
        
        // Note: Real audit log would be handled via interceptors or decorator later
        
        return Result.Success(customer.Id);
    }
}
