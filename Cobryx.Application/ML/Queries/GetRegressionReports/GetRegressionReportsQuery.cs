using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Decision;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.ML.Queries.GetRegressionReports;

public record GetRegressionReportsQuery(int Limit = 20) : IRequest<Result<List<RegressionReport>>>;

public class GetRegressionReportsHandler(
    ICobryxDbContext db) : IRequestHandler<GetRegressionReportsQuery, Result<List<RegressionReport>>>
{
    public async Task<Result<List<RegressionReport>>> Handle(GetRegressionReportsQuery request, CancellationToken cancellationToken)
    {
        List<RegressionReport> reports = await db.RegressionReports
            .OrderByDescending(r => r.RunAt)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        return Result.Success(reports);
    }
}
