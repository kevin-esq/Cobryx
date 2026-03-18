using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Events.Lending;

public record AccrualPostedEvent(
    Guid LoanId,
    long LedgerSequenceId,
    DateTime OccurredOn
) : IDomainEvent;

public record LoanDisbursedEvent(
    Guid LoanId,
    long LedgerSequenceId,
    DateTime OccurredOn
) : IDomainEvent;

public record LoanStatusChangedEvent(
    Guid LoanId,
    long LedgerSequenceId,
    DateTime OccurredOn
) : IDomainEvent;

public record LoanPaymentAppliedEvent(
    Guid LoanId,
    long LedgerSequenceId,
    DateTime OccurredOn
) : IDomainEvent;
