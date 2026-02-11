using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Customers.Common;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Exceptions.Customers;
using Cobryx.Domain.Exceptions.Tenants;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Customers.Queries.GetCustomerById;

public record GetCustomerByIdQuery(Guid Id) : IRequest<Result<CustomerDto>>;

public class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetCustomerByIdHandler(ICustomerRepository customerRepository, ITenantProvider tenantProvider)
    {
        _customerRepository = customerRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) throw new TenantContextMissingException();

        var customer = await _customerRepository.Query()
            .AsNoTracking()
            .Include(c => c.Credits)
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.TenantId == tenantId.Value, cancellationToken);

        if (customer == null)
        {
            throw new CustomerNotFoundException(request.Id);
        }

        return Result.Success(new CustomerDto(
            customer.Id,
            customer.FirstName,
            customer.LastName,
            customer.FullName,
            customer.Phone,
            customer.Address,
            customer.Document,
            customer.Credits.Count(c => c.Status == CreditStatus.Active),
            customer.CreatedAt));
    }
}
