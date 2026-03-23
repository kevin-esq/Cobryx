using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Payments.Queries.GetPaymentLinkByToken;

public record PaymentLinkDto(
    Guid Id,
    string CustomerName,
    Cobryx.Domain.ValueObjects.Money Amount,
    string Status,
    DateTime ExpiresAt,
    string? LoanNumber = null,
    string? StripeClientSecret = null);

public record GetPaymentLinkByTokenQuery(string Token) : IRequest<Result<PaymentLinkDto>>;

public class GetPaymentLinkByTokenHandler : IRequestHandler<GetPaymentLinkByTokenQuery, Result<PaymentLinkDto>>
{
    private readonly ICobryxDbContext _context;
    private readonly StripeOptions _stripeOptions;

    public GetPaymentLinkByTokenHandler(ICobryxDbContext context, IOptions<StripeOptions> stripeOptions)
    {
        _context = context;
        _stripeOptions = stripeOptions.Value;
    }

    public async Task<Result<PaymentLinkDto>> Handle(GetPaymentLinkByTokenQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return Result.Failure<PaymentLinkDto>(DomainErrorCode.Auth.TokenMissing);

        var parts = request.Token.Split('.', 2);
        if (parts.Length != 2)
            return Result.Failure<PaymentLinkDto>(DomainErrorCode.PaymentLink.NotFound);

        var salt = parts[0];
        var rawToken = parts[1];

        var link = await _context.PaymentLinks
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.Salt == salt, ct);

        if (link == null)
            return Result.Failure<PaymentLinkDto>(DomainErrorCode.PaymentLink.NotFound);

        if (!link.ValidateToken(rawToken, _stripeOptions.PaymentLinkSecret))
        {
            await _context.SaveChangesAsync(ct);
            return Result.Failure<PaymentLinkDto>(DomainErrorCode.PaymentLink.InvalidStatus);
        }

        if (link.Status == PaymentLinkStatus.Paid)
            return Result.Failure<PaymentLinkDto>(DomainErrorCode.PaymentLink.AlreadyPaid);

        if (link.ExpiresAt < DateTime.UtcNow)
        {
            link.Expire();
            await _context.SaveChangesAsync(ct);
            return Result.Failure<PaymentLinkDto>(DomainErrorCode.PaymentLink.Expired);
        }

        var dto = new PaymentLinkDto(
            link.Id,
            $"{link.Customer.FirstName} {link.Customer.LastName}",
            link.AmountSnapshot,
            link.Status.ToString(),
            link.ExpiresAt,
            link.LoanId.HasValue ? "Loan Info" : null // We can expand this with a join if needed
        );

        return Result.Success(dto);
    }
}
