using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities.Lending.Enums;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Lending.Queries.GetAgingReport;

public record GetAgingReportQuery : IRequest<AgingReportResponse>
{
    public Guid TenantId { get; init; }
}

public record AgingReportResponse
{
    public decimal TotalPrincipalOutstanding { get; init; }
    public decimal PAR30 { get; init; } // Portfolio at Risk > 30 days (%)
    public decimal PAR90 { get; init; } // Portfolio at Risk > 90 days (%)
    public decimal NPLRatio { get; init; } // Non-Performing Loan Ratio (%)

    public List<AgingBucket> Buckets { get; init; } = new();
}

public record AgingBucket
{
    public string Label { get; init; } = string.Empty;
    public int LoanCount { get; init; }
    public decimal PrincipalAmount { get; init; }
    public decimal ArrearsAmount { get; init; }
}

public class GetAgingReportHandler : IRequestHandler<GetAgingReportQuery, AgingReportResponse>
{
    private readonly ICobryxDbContext _context;

    public GetAgingReportHandler(ICobryxDbContext context)
    {
        _context = context;
    }

    public async Task<AgingReportResponse> Handle(GetAgingReportQuery request, CancellationToken ct)
    {
        var loans = await _context.Loans
            .Where(l => l.TenantId == request.TenantId && l.Status == LoanStatus.Active)
            .ToListAsync(ct);

        if (!loans.Any())
            return new AgingReportResponse();

        var totalPrincipal = loans.Sum(l => l.CurrentPrincipalBalance);

        var par30Principal = loans
            .Where(l => l.FinancialDaysPastDue > 30)
            .Sum(l => l.CurrentPrincipalBalance);

        var par90Principal = loans
            .Where(l => l.FinancialDaysPastDue > 90)
            .Sum(l => l.CurrentPrincipalBalance);

        var defaultedPrincipal = loans
            .Where(l => l.FinancialStatus == FinancialStatus.Default || l.FinancialStatus == FinancialStatus.ChargedOff)
            .Sum(l => l.CurrentPrincipalBalance);

        var buckets = new List<AgingBucket>
        {
            CreateBucket("Current (0-30)", loans.Where(l => l.FinancialDaysPastDue <= 30)),
            CreateBucket("Late (31-60)", loans.Where(l => l.FinancialDaysPastDue > 30 && l.FinancialDaysPastDue <= 60)),
            CreateBucket("Delinquent (61-90)", loans.Where(l => l.FinancialDaysPastDue > 60 && l.FinancialDaysPastDue <= 90)),
            CreateBucket("Default (91-180)", loans.Where(l => l.FinancialDaysPastDue > 90 && l.FinancialDaysPastDue <= 180)),
            CreateBucket("Critical (181+)", loans.Where(l => l.FinancialDaysPastDue > 180))
        };

        return new AgingReportResponse
        {
            TotalPrincipalOutstanding = totalPrincipal,
            PAR30 = totalPrincipal > 0 ? (par30Principal / totalPrincipal) * 100 : 0,
            PAR90 = totalPrincipal > 0 ? (par90Principal / totalPrincipal) * 100 : 0,
            NPLRatio = totalPrincipal > 0 ? (defaultedPrincipal / totalPrincipal) * 100 : 0,
            Buckets = buckets
        };
    }

    private AgingBucket CreateBucket(string label, IEnumerable<Domain.Entities.Lending.Loan> loans)
    {
        var loanList = loans.ToList();
        return new AgingBucket
        {
            Label = label,
            LoanCount = loanList.Count,
            PrincipalAmount = loanList.Sum(l => l.CurrentPrincipalBalance),
            ArrearsAmount = loanList.Sum(l => l.ArrearsAmount)
        };
    }
}
