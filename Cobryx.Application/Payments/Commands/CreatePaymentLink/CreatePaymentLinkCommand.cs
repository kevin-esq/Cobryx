using System.Security.Cryptography;

using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant == null || tenant.IsPaymentRestricted)
        {
            return Result.Failure<string>(DomainErrorCode.Common.GeneralError);
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.TenantId == tenantId, ct);

        if (customer == null)
            return Result.Failure<string>(DomainErrorCode.Customer.NotFound);

        if (request.LoanId.HasValue)
        {
            var loanExists = await _context.Loans
                .AnyAsync(l => l.Id == request.LoanId && l.TenantId == tenantId, ct);
            if (!loanExists)
                return Result.Failure<string>(DomainErrorCode.Loans.NotFound);
        }

        if (!string.IsNullOrWhiteSpace(request.ExternalReference))
        {
            var existingLink = await _context.PaymentLinks
                .FirstOrDefaultAsync(pl => pl.TenantId == tenantId && pl.ExternalReference == request.ExternalReference && pl.Status == PaymentLinkStatus.Active, ct);

            if (existingLink != null)
            {
                existingLink.Expire();
            }
        }

        var rawToken = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddDays(request.ExpiryDays ?? 7);

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

        return Result.Success($"{paymentLink.Salt}.{rawToken}");
    }

    private static string GenerateSecureToken()
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
