using System;
using System.Collections.Generic;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.ValueObjects;

public record Money : ValueObject
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    private Money()
    {
        Currency = null!;
    }

    public Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException(DomainErrorCode.ValueObjects.InvalidCurrency);

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = CobryxDefaults.Currency) => new(0, currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new DomainException(DomainErrorCode.ValueObjects.CurrencyMismatch);
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new DomainException(DomainErrorCode.ValueObjects.CurrencyMismatch);
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
