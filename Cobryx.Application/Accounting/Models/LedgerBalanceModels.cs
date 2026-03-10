namespace Cobryx.Application.Accounting.Models;

public record CachedBalance(
    decimal Balance,
    long LastJournalSequenceId,
    DateTime CachedAt
);
