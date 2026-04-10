using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Decision;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.ML.Queries.GetRegressionReport;

public record GetRegressionReportQuery(Guid Id) : IRequest<Result<RegressionReport>>;

public class GetRegressionReportHandler(
    ICobryxDbContext db) : IRequestHandler<GetRegressionReportQuery, Result<RegressionReport>>
{
    public async Task<Result<RegressionReport>> Handle(GetRegressionReportQuery request, CancellationToken cancellationToken)
    {
        var report = await db.RegressionReports
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (report == null)
            return Result.Failure<RegressionReport>(DomainErrorCode.Common.EntityNotFound);

        return Result.Success(report);
    }
}
