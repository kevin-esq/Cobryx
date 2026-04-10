using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Payments.Queries.GetPaymentLinkByToken
{
    public record PaymentLinkDto(
        Guid Id,
        string CustomerName,
        Domain.ValueObjects.Money Amount,
        string Status,
        DateTime ExpiresAt,
        string? LoanNumber = null,
        string? StripeClientSecret = null);

    public record GetPaymentLinkByTokenQuery(string Token) : IRequest<Result<PaymentLinkDto>>;

    public class GetPaymentLinkByTokenHandler(ICobryxDbContext context, IClock clock, IOptions<StripeOptions> stripeOptions) : IRequestHandler<GetPaymentLinkByTokenQuery, Result<PaymentLinkDto>>
    {
        private readonly ICobryxDbContext _context = context;
        private readonly IClock _clock = clock;
        private readonly StripeOptions _stripeOptions = stripeOptions.Value;

        public async Task<Result<PaymentLinkDto>> Handle(GetPaymentLinkByTokenQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return Result.Failure<PaymentLinkDto>(DomainErrorCode.Auth.TokenMissing);
            }

            var parts = request.Token.Split('.', 2);
            if (parts.Length != 2)
            {
                return Result.Failure<PaymentLinkDto>(DomainErrorCode.PaymentLink.NotFound);
            }

            var salt = parts[0];
            var rawToken = parts[1];

            var link = await _context.PaymentLinks
                .Include(l => l.Customer)
                .FirstOrDefaultAsync(l => l.Salt == salt, cancellationToken);

            if (link == null)
            {
                return Result.Failure<PaymentLinkDto>(DomainErrorCode.PaymentLink.NotFound);
            }

            if (!link.ValidateToken(rawToken, _stripeOptions.PaymentLinkSecret))
            {
                _ = await _context.SaveChangesAsync(cancellationToken);
                return Result.Failure<PaymentLinkDto>(DomainErrorCode.PaymentLink.InvalidStatus);
            }

            if (link.Status == PaymentLinkStatus.Paid)
            {
                return Result.Failure<PaymentLinkDto>(DomainErrorCode.PaymentLink.AlreadyPaid);
            }

            if (link.ExpiresAt < _clock.UtcNow)
            {
                link.Expire();
                _ = await _context.SaveChangesAsync(cancellationToken);
                return Result.Failure<PaymentLinkDto>(DomainErrorCode.PaymentLink.Expired);
            }

            var dto = new PaymentLinkDto(
                link.Id,
                $"{link.Customer.FirstName} {link.Customer.LastName}",
                link.AmountSnapshot,
                link.Status.ToString(),
                link.ExpiresAt,
                link.LoanId.HasValue ? "Loan Info" : null
            );

            return Result.Success(dto);
        }
    }
}
