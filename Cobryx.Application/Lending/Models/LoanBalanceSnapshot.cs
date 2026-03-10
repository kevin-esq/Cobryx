namespace Cobryx.Application.Lending.Models;

public record LoanBalanceSnapshot(
    decimal Principal,
    decimal Interest,
    decimal Fees,
    long LedgerSequence
);
