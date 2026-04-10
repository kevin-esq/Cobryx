using System.Security.Cryptography;

using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Payments.Commands.CreatePaymentLink
{
    [TenantScoped]
    public record CreatePaymentLinkCommand(
        Guid CustomerId,
        Money Amount,
        Guid? LoanId = null,
        string? ExternalReference = null,
        int? ExpiryDays = 7) : IRequest<Result<string>>, IRequiresTenant;

    public class CreatePaymentLinkHandler(
        ICobryxDbContext context,
        ITenantProvider tenantProvider,
        IClock clock,
        IOptions<StripeOptions> stripeOptions) : IRequestHandler<CreatePaymentLinkCommand, Result<string>>
    {
        private readonly StripeOptions _stripeOptions = stripeOptions.Value;

        public async Task<Result<string>> Handle(CreatePaymentLinkCommand request, CancellationToken cancellationToken)
        {
            var tenantId = tenantProvider.GetTenantId();
            if (tenantId == null || tenantId == Guid.Empty)
            {
                return Result.Failure<string>(DomainErrorCode.Common.UnauthorizedContext);
            }

            var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
            if (tenant == null || tenant.IsPaymentRestricted)
            {
                return Result.Failure<string>(DomainErrorCode.Common.GeneralError);
            }

            var customer = await context.Customers
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.TenantId == tenantId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<string>(DomainErrorCode.Customer.NotFound);
            }

            if (request.LoanId.HasValue)
            {
                var loanExists = await context.Loans
                    .AnyAsync(l => l.Id == request.LoanId && l.TenantId == tenantId, cancellationToken);
                if (!loanExists)
                {
                    return Result.Failure<string>(DomainErrorCode.Loans.NotFound);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.ExternalReference))
            {
                var existingLink = await context.PaymentLinks
                    .FirstOrDefaultAsync(pl => pl.TenantId == tenantId && pl.ExternalReference == request.ExternalReference && pl.Status == PaymentLinkStatus.Active, cancellationToken);

                existingLink?.Expire();
            }

            var rawToken = GenerateSecureToken();
            var expiresAt = clock.UtcNow.AddDays(request.ExpiryDays ?? 7);

            var paymentLink = new PaymentLink(
                tenantId.Value,
                request.CustomerId,
                request.Amount,
                rawToken,
                expiresAt,
                _stripeOptions.PaymentLinkSecret,
                clock.UtcNow,
                request.LoanId,
                request.ExternalReference
            );

            _ = context.PaymentLinks.Add(paymentLink);
            _ = await context.SaveChangesAsync(cancellationToken);

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
    [..32];
        }
    }
}
