using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Customers.Commands.PaymentMethods;

public record AttachPaymentMethodCommand(Guid CustomerId, string PaymentMethodId) : IRequest<Result>;

public class AttachPaymentMethodHandler(
    ICobryxDbContext dbContext,
    IStripeService stripeService,
    ILogger<AttachPaymentMethodHandler> logger)
    : IRequestHandler<AttachPaymentMethodCommand, Result>
{
    public async Task<Result> Handle(AttachPaymentMethodCommand request, CancellationToken ct)
    {
        Customer? customer = await dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);

        if (customer == null)
            return Result.Failure(DomainErrorCode.Customer.NotFound);

        try
        {
            if (string.IsNullOrEmpty(customer.StripeCustomerId))
            {
                var stripeId = await stripeService.CreateCustomerAsync(customer.Email, customer.FullName, ct);
                customer.SetStripeCustomerId(stripeId);
            }

            await stripeService.AttachPaymentMethodAsync(customer.StripeCustomerId!, request.PaymentMethodId, ct);

            customer.SetDefaultPaymentMethod(request.PaymentMethodId);

            await dbContext.SaveChangesAsync(ct);

            logger.LogInformation("PaymentMethod {pmId} attached and set as default for Customer {CustomerId}",
                request.PaymentMethodId, request.CustomerId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to attach PaymentMethod {pmId} to Customer {CustomerId}",
                request.PaymentMethodId, request.CustomerId);
            return Result.Failure(DomainErrorCode.System.InternalError);
        }
    }
}
