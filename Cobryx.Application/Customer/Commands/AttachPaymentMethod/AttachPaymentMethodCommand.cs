using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Customer.Commands.AttachPaymentMethod;

public record AttachPaymentMethodCommand(Guid CustomerId, string PaymentMethodId) : IRequest<Result>;

public class AttachPaymentMethodHandler : IRequestHandler<AttachPaymentMethodCommand, Result>
{
    private readonly ICobryxDbContext _dbContext;
    private readonly IStripeService _stripeService;
    private readonly ILogger<AttachPaymentMethodHandler> _logger;

    public AttachPaymentMethodHandler(
        ICobryxDbContext dbContext,
        IStripeService stripeService,
        ILogger<AttachPaymentMethodHandler> logger)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task<Result> Handle(AttachPaymentMethodCommand request, CancellationToken ct)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);

        if (customer == null)
            return Result.Failure(DomainErrorCode.Customer.NotFound);

        try
        {
            // 1. Ensure Stripe Customer exists
            if (string.IsNullOrEmpty(customer.StripeCustomerId))
            {
                var stripeId = await _stripeService.CreateCustomerAsync(customer.Email, customer.FullName, ct);
                customer.SetStripeCustomerId(stripeId);
            }

            // 2. Attach Payment Method in Stripe
            await _stripeService.AttachPaymentMethodAsync(customer.StripeCustomerId!, request.PaymentMethodId, ct);

            // 3. Mark as Default if it's the first or per application policy
            // In this version, we set as default to enable AutoPay path easily
            customer.SetDefaultPaymentMethod(request.PaymentMethodId);

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("PaymentMethod {pmId} attached and set as default for Customer {CustomerId}",
                request.PaymentMethodId, request.CustomerId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to attach PaymentMethod {pmId} to Customer {CustomerId}",
                request.PaymentMethodId, request.CustomerId);
            return Result.Failure(DomainErrorCode.System.InternalError);
        }
    }
}
