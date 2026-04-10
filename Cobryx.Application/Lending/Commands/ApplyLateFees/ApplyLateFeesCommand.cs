using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Lending.Commands.ApplyLateFees;

/// <summary>
/// Command to process overdue installments and apply late fees.
/// Can be triggered for a specific loan or for all active loans of a tenant.
/// </summary>
// * TenantId parameter is IGNORED by handler for security.
// Tenant context is resolved via ITenantProvider.
// This parameter is deprecated and will be removed in future refactor.
[TenantScoped]
public record ApplyLateFeesCommand(Guid? LoanId = null, Guid? TenantId = null) : IRequest<Result<LateFeeResultDto>>, IRequiresTenant;

public record LateFeeResultDto(
    int LoansProcessed,
    int InstallmentsAffected,
    decimal TotalFeesApplied
);
