using System.Text;

using Cobryx.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.ML;

public class DatasetExporter
{
    private readonly ICobryxDbContext _db;

    public DatasetExporter(ICobryxDbContext db)
    {
        _db = db;
    }

    public async Task ExportAsync(string path)
    {
        var data = await _db.LoanBalanceSnapshots
            .OrderBy(x => x.RecordedAt)
            .ToListAsync();

        var rows = data.Select(x => new
        {
            utilization = x.PrincipalBalance / (x.PrincipalBalance + 1m),
            paymentDelay = 0m,
            behaviorScore = 0.5m,
            dpdTrend = (decimal)x.DaysPastDue,
            outstanding = x.PrincipalBalance + x.InterestBalance + x.LateFeeBalance,
            label = x.DaysPastDue >= 30 ? 1 : 0
        });

        var dir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
        {
            System.IO.Directory.CreateDirectory(dir);
        }

        var csv = new StringBuilder();
        csv.AppendLine("utilization,paymentDelay,behaviorScore,dpdTrend,outstanding,default");

        foreach (var r in rows)
        {
            csv.AppendLine(System.FormattableString.Invariant($"{r.utilization:F4},{r.paymentDelay:F4},{r.behaviorScore:F4},{r.dpdTrend:F4},{r.outstanding:F4},{r.label}"));
        }

        await System.IO.File.WriteAllTextAsync(path, csv.ToString());
    }
}
