using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Payments.Webhooks.Commands.HandlePaymentFailed;

public record HandlePaymentFailedCommand(JsonElement StripeObject, string EventType) : IRequest<Result>;

public class HandlePaymentFailedHandler : IRequestHandler<HandlePaymentFailedCommand, Result>
{
    private readonly ICobryxDbContext _dbContext;
    private readonly IPaymentOrchestrationService _orchestrationService;

    public HandlePaymentFailedHandler(ICobryxDbContext dbContext, IPaymentOrchestrationService orchestrationService)
    {
        _dbContext = dbContext;
        _orchestrationService = orchestrationService;
    }

    public async Task<Result> Handle(HandlePaymentFailedCommand request, CancellationToken ct)
    {
        if (!request.StripeObject.TryGetProperty("metadata", out var metadata) ||
            !metadata.TryGetProperty("CustomerId", out var customerIdProp) ||
            !Guid.TryParse(customerIdProp.GetString(), out var customerId))
        {
            if (request.StripeObject.TryGetProperty("customer", out var stripeCustIdProp))
            {
                var stripeCustId = stripeCustIdProp.GetString();
                var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.StripeCustomerId == stripeCustId, ct);
                if (customer == null)
                    return Result.Failure(DomainErrorCode.Customer.NotFound);
                customerId = customer.Id;
            }
            else
            {
                return Result.Failure(DomainErrorCode.Webhooks.InvalidDataFormat);
            }
        }

        decimal amount = 0;
        string currency = "USD";
        string? failureCode = null;
        string description = "Payment attempt failed.";

        if (request.EventType.Contains("invoice"))
        {
            amount = request.StripeObject.GetProperty("amount_due").GetInt64() / 100m;
            currency = request.StripeObject.GetProperty("currency").GetString()?.ToUpper() ?? "USD";
            failureCode = "invoice_payment_failed";
            description = $"Invoice {request.StripeObject.GetProperty("number").GetString()} payment failed.";
        }
        else
        {
            amount = request.StripeObject.GetProperty("amount").GetInt64() / 100m;
            currency = request.StripeObject.GetProperty("currency").GetString()?.ToUpper() ?? "USD";

            if (request.StripeObject.TryGetProperty("last_payment_error", out var errorProp))
            {
                failureCode = errorProp.TryGetProperty("code", out var code) ? code.GetString() : null;
            }
        }

        var stripePaymentIntentId = request.StripeObject.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

        return await _orchestrationService.HandlePaymentFailureAsync(
            customerId,
            failureCode,
            amount,
            currency,
            description,
            stripePaymentIntentId,
            null,
            ct);
    }
}
