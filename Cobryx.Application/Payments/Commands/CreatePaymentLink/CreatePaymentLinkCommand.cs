using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Enums;
using Concordia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Cobryx.Infrastructure.Configuration;
using System.Security.Cryptography;

namespace Cobryx.Application.Payments.Commands.CreatePaymentLink;

public record CreatePaymentLinkCommand(
    Guid CustomerId,
    Money Amount,
    Guid? LoanId = null,
    string? ExternalReference = null,
    int? ExpiryDays = 7) : IRequest<Result<string>>;

public class CreatePaymentLinkHandler : IRequestHandler<CreatePaymentLinkCommand, Result<string>>
{
    private readonly ICobryxDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly StripeOptions _stripeOptions;

    public CreatePaymentLinkHandler(
        ICobryxDbContext context, 
        ITenantProvider tenantProvider,
        IOptions<StripeOptions> stripeOptions)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _stripeOptions = stripeOptions.Value;
    }

    public async Task<Result<string>> Handle(CreatePaymentLinkCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null || tenantId == Guid.Empty)
            return Result.Failure<string>(DomainErrorCode.Common.UnauthorizedContext);

        // 1. Validate Customer
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.TenantId == tenantId, ct);
        
        if (customer == null)
            return Result.Failure<string>(DomainErrorCode.Customer.NotFound);

        // 2. Validate Loan (if provided)
        if (request.LoanId.HasValue)
        {
            var loanExists = await _context.Credits
                .AnyAsync(l => l.Id == request.LoanId && l.TenantId == tenantId, ct);
            if (!loanExists)
                return Result.Failure<string>(DomainErrorCode.Loans.NotFound);
        }

        // 3. Idempotency check for ExternalReference
        if (!string.IsNullOrWhiteSpace(request.ExternalReference))
        {
            var existingLink = await _context.PaymentLinks
                .FirstOrDefaultAsync(pl => pl.TenantId == tenantId && pl.ExternalReference == request.ExternalReference && pl.Status == PaymentLinkStatus.Active, ct);
            
            if (existingLink != null)
            {
                // Re-hashing is the only way to get a new raw token if needed, 
                // but usually we just want to return the existence or rotate.
                // For now, let's return the existing one if we can't get the raw token (we can't since it's hashed).
                // Protocol: If exists, we expire the old one and create a new one to provide a fresh token.
                existingLink.Expire();
            }
        }

        // 4. Generate Raw Token
        var rawToken = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddDays(request.ExpiryDays ?? 7);

        // 5. Create Payment Link
        var paymentLink = new PaymentLink(
            tenantId.Value,
            request.CustomerId,
            request.Amount,
            rawToken,
            expiresAt,
            _stripeOptions.PaymentLinkSecret,
            request.LoanId,
            request.ExternalReference
        );

        _context.PaymentLinks.Add(paymentLink);
        await _context.SaveChangesAsync(ct);

        // 6. Return bipartite token (Salt + RawToken)
        // This allows efficient lookup by Salt, then verification by HMAC
        return Result.Success($"{paymentLink.Salt}.{rawToken}");
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, 32);
    }
}
