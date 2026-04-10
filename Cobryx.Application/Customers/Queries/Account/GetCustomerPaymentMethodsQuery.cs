using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Customers.Queries.Account;

public record GetCustomerPaymentMethodsQuery(Guid CustomerId) : IRequest<Result<List<CustomerPaymentMethodDto>>>;

public record CustomerPaymentMethodDto(
    string Id,
    string Brand,
    string Last4,
    short ExpMonth,
    short ExpYear,
    bool IsDefault);

public class GetCustomerPaymentMethodsHandler(ICobryxDbContext dbContext, IStripeService stripeService)
    : IRequestHandler<GetCustomerPaymentMethodsQuery, Result<List<CustomerPaymentMethodDto>>>
{
    public async Task<Result<List<CustomerPaymentMethodDto>>> Handle(GetCustomerPaymentMethodsQuery request, CancellationToken ct)
    {
        Customer? customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);

        if (customer == null)
            return Result.Failure<List<CustomerPaymentMethodDto>>(DomainErrorCode.Customer.NotFound);

        if (string.IsNullOrEmpty(customer.StripeCustomerId))
            return Result.Success(new List<CustomerPaymentMethodDto>());

        List<StripePaymentMethodDto> stripeMethods = await stripeService.ListPaymentMethodsAsync(customer.StripeCustomerId, ct);

        var dtos = stripeMethods.Select(m => new CustomerPaymentMethodDto(
            Id: m.Id,
            Brand: m.Brand,
            Last4: m.Last4,
            ExpMonth: m.ExpMonth,
            ExpYear: m.ExpYear,
            IsDefault: m.Id == customer.DefaultPaymentMethodId
        )).ToList();

        return Result.Success(dtos);
    }
}
