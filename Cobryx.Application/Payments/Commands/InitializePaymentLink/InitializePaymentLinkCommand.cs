using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Cobryx.Infrastructure.Configuration;

namespace Cobryx.Application.Payments.Commands.InitializePaymentLink;

public record InitializePaymentLinkCommand(string Token) : IRequest<Result<string>>;

public class InitializePaymentLinkHandler : IRequestHandler<InitializePaymentLinkCommand, Result<string>>
{
    private readonly ICobryxDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly StripeOptions _stripeOptions;

    public InitializePaymentLinkHandler(
        ICobryxDbContext context, 
        IStripeService stripeService,
        IOptions<StripeOptions> stripeOptions)
    {
        _context = context;
        _stripeService = stripeService;
        _stripeOptions = stripeOptions.Value;
    }

    public async Task<Result<string>> Handle(InitializePaymentLinkCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.Failure<string>(DomainErrorCode.Auth.TokenMissing);

        // 1. Parse Bipartite Token: {Salt}.{RawToken}
        var parts = request.Token.Split('.', 2);
        if (parts.Length != 2)
            return Result.Failure<string>(DomainErrorCode.PaymentLink.NotFound);

        var salt = parts[0];
        var rawToken = parts[1];

        // 2. Efficient Lookup by Salt
        var link = await _context.PaymentLinks
            .FirstOrDefaultAsync(l => l.Salt == salt, ct);

        if (link == null)
            return Result.Failure<string>(DomainErrorCode.PaymentLink.NotFound);

        // 3. Secure Verification with Server Secret
        if (!link.ValidateToken(rawToken, _stripeOptions.PaymentLinkSecret))
        {
            await _context.SaveChangesAsync(ct);
            return Result.Failure<string>(DomainErrorCode.PaymentLink.InvalidStatus);
        }

        // 4. Return existing secret if already processing (Intent Reuse)
        if (link.Status == PaymentLinkStatus.Processing && !string.IsNullOrEmpty(link.StripePaymentIntentId))
        {
            try 
            {
                var secret = await _stripeService.GetPaymentIntentClientSecretAsync(link.StripePaymentIntentId, ct);
                return Result.Success(secret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve existing client secret for Intent {IntentId}", link.StripePaymentIntentId);
                // Fallback: Create new intent if original is lost/invalid
            }
        }

        // 3. Create Stripe PaymentIntent
        var metadata = new Dictionary<string, string>
        {
            { "payment_link_id", link.Id.ToString() },
            { "tenant_id", link.TenantId.ToString() },
            { "customer_id", link.CustomerId.ToString() },
            { "loan_id", link.LoanId?.ToString() ?? "N/A" },
            { "source", "cobryx_payment_link" }
        };

        var (intentId, clientSecret) = await _stripeService.CreatePaymentIntentAsync(
            link.AmountSnapshot,
            metadata,
            ct);

        // 4. Update Link State
        link.MarkAsProcessing(intentId);
        
        await _context.SaveChangesAsync(ct);

        return Result.Success(clientSecret);
    }
}
