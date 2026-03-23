using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Payments.Commands.InitializePaymentLink;

public record InitializePaymentLinkCommand(string Token) : IRequest<Result<string>>;

public class InitializePaymentLinkHandler : IRequestHandler<InitializePaymentLinkCommand, Result<string>>
{
    private readonly ICobryxDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<InitializePaymentLinkHandler> _logger;

    public InitializePaymentLinkHandler(
        ICobryxDbContext context,
        IStripeService stripeService,
        IOptions<StripeOptions> stripeOptions,
        ILogger<InitializePaymentLinkHandler> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _stripeOptions = stripeOptions.Value;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(InitializePaymentLinkCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.Failure<string>(DomainErrorCode.Auth.TokenMissing);

        var parts = request.Token.Split('.', 2);
        if (parts.Length != 2)
            return Result.Failure<string>(DomainErrorCode.PaymentLink.NotFound);

        var salt = parts[0];
        var rawToken = parts[1];

        var link = await _context.PaymentLinks
            .FirstOrDefaultAsync(l => l.Salt == salt, ct);

        if (link == null)
            return Result.Failure<string>(DomainErrorCode.PaymentLink.NotFound);

        if (!link.ValidateToken(rawToken, _stripeOptions.PaymentLinkSecret))
        {
            await _context.SaveChangesAsync(ct);
            return Result.Failure<string>(DomainErrorCode.PaymentLink.InvalidStatus);
        }

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == link.TenantId, ct);
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
                var status = await _stripeService.GetPaymentIntentStatusAsync(link.StripePaymentIntentId, ct);
                if (status is "requires_payment_method" or "requires_confirmation" or "requires_action" or "processing")
                {
                    var secret = await _stripeService.GetPaymentIntentClientSecretAsync(link.StripePaymentIntentId, ct);
                    return Result.Success(secret);
                }

                _logger.LogWarning("Existing Intent {IntentId} has status {Status}. Generating fresh intent.",
                    link.StripePaymentIntentId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify existing intent {IntentId}. Falling back to new intent.",
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

        var (intentId, clientSecret) = await _stripeService.CreatePaymentIntentAsync(
            link.AmountSnapshot,
            metadata,
            tenant.IsConnectActive ? tenant.StripeAccountId : null,
            appFee,
            ct);

        link.MarkAsProcessing(intentId);

        await _context.SaveChangesAsync(ct);

        return Result.Success(clientSecret);
    }
}
