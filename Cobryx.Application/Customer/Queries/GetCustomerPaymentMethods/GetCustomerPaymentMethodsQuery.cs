using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Customer.Queries.GetCustomerPaymentMethods;

public record GetCustomerPaymentMethodsQuery(Guid CustomerId) : IRequest<Result<List<CustomerPaymentMethodDto>>>;

public record CustomerPaymentMethodDto(
    string Id,
    string Brand,
    string Last4,
    short ExpMonth,
    short ExpYear,
    bool IsDefault);

public class GetCustomerPaymentMethodsHandler : IRequestHandler<GetCustomerPaymentMethodsQuery, Result<List<CustomerPaymentMethodDto>>>
{
    private readonly ICobryxDbContext _dbContext;
    private readonly IStripeService _stripeService;

    public GetCustomerPaymentMethodsHandler(ICobryxDbContext dbContext, IStripeService stripeService)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
    }

    public async Task<Result<List<CustomerPaymentMethodDto>>> Handle(GetCustomerPaymentMethodsQuery request, CancellationToken ct)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);

        if (customer == null)
            return Result.Failure<List<CustomerPaymentMethodDto>>(DomainErrorCode.Customer.NotFound);

        if (string.IsNullOrEmpty(customer.StripeCustomerId))
            return Result.Success(new List<CustomerPaymentMethodDto>());

        var stripeMethods = await _stripeService.ListPaymentMethodsAsync(customer.StripeCustomerId, ct);

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
