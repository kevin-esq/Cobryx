using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Payments.Commands.InitializePaymentLink;

// * PUBLIC ENDPOINT - Not tenant-scoped
// This command is used by external payers via payment link token.
// Tenant context is derived from the PaymentLink entity, not from request context.
// DO NOT add [TenantScoped] or IRequiresTenant.
public record InitializePaymentLinkCommand(string Token) : IRequest<Result<string>>;

public class InitializePaymentLinkHandler(
    ICobryxDbContext context,
    IStripeService stripeService,
    IOptions<StripeOptions> stripeOptions,
    IClock clock,
    ILogger<InitializePaymentLinkHandler> logger)
    : IRequestHandler<InitializePaymentLinkCommand, Result<string>>
{
    private readonly StripeOptions _stripeOptions = stripeOptions.Value;

    public async Task<Result<string>> Handle(InitializePaymentLinkCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.Failure<string>(DomainErrorCode.Auth.TokenMissing);

        var parts = request.Token.Split('.', 2);
        if (parts.Length != 2)
            return Result.Failure<string>(DomainErrorCode.PaymentLink.NotFound);

        var salt = parts[0];
        var rawToken = parts[1];

        PaymentLink? link = await context.PaymentLinks
            .FirstOrDefaultAsync(l => l.Salt == salt, ct);

        if (link == null)
            return Result.Failure<string>(DomainErrorCode.PaymentLink.NotFound);

        if (!link.ValidateToken(rawToken, _stripeOptions.PaymentLinkSecret))
        {
            await context.SaveChangesAsync(ct);
            return Result.Failure<string>(DomainErrorCode.PaymentLink.InvalidStatus);
        }

        Tenant? tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == link.TenantId, ct);
        if (tenant == null)
            return Result.Failure<string>(DomainErrorCode.Common.GeneralError);

        if (!tenant.IsConnectActive && !string.IsNullOrEmpty(tenant.StripeAccountId))
        {
            return Result.Failure<string>(DomainErrorCode.PaymentLink.ConnectNotActive);
        }

        if (link.Status == PaymentLinkStatus.Processing && !string.IsNullOrEmpty(link.StripePaymentIntentId))
        {
            try
            {
                var status = await stripeService.GetPaymentIntentStatusAsync(link.StripePaymentIntentId, ct);
                if (status is StripeConstants.PaymentIntentStatuses.RequiresPaymentMethod
                    or StripeConstants.PaymentIntentStatuses.RequiresConfirmation
                    or StripeConstants.PaymentIntentStatuses.RequiresAction
                    or StripeConstants.PaymentIntentStatuses.Processing)
                {
                    var secret = await stripeService.GetPaymentIntentClientSecretAsync(link.StripePaymentIntentId, ct);
                    return Result.Success(secret);
                }

                logger.LogWarning("Existing Intent {IntentId} has status {Status}. Generating fresh intent.",
                    link.StripePaymentIntentId, status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to verify existing intent {IntentId}. Falling back to new intent.",
                    link.StripePaymentIntentId);
            }
        }

        var metadata = new Dictionary<string, string>
        {
            { "payment_link_id", link.Id.ToString() },
            { "tenant_id", link.TenantId.ToString() },
            { "customer_id", link.CustomerId.ToString() },
            { "loan_id", link.LoanId?.ToString() ?? "N/A" },
            { "source", "cobryx_payment_link" }
        };

        decimal? appFee = null;
        if (tenant.IsConnectActive)
        {
            appFee = Math.Round(link.AmountSnapshot.Amount * 0.015m, 2);
        }

        var (intentId, clientSecret) = await stripeService.CreatePaymentIntentAsync(
            link.AmountSnapshot,
            metadata,
            tenant.IsConnectActive ? tenant.StripeAccountId : null,
            appFee,
            ct);

        link.MarkAsProcessing(intentId, clock.UtcNow);

        await context.SaveChangesAsync(ct);

        return Result.Success(clientSecret);
    }
}
