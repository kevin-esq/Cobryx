using Cobryx.Domain.Common;
using Concordia;

namespace Cobryx.Application.Lending.Commands.ApplyLateFees;

/// <summary>
/// Command to process overdue installments and apply late fees.
/// Can be triggered for a specific loan or for all active loans of a tenant.
/// </summary>
public record ApplyLateFeesCommand(Guid? LoanId = null, Guid? TenantId = null) : IRequest<Result<LateFeeResultDto>>;

public record LateFeeResultDto(
    int LoansProcessed,
    int InstallmentsAffected,
    decimal TotalFeesApplied
);
