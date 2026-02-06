using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.ValueObjects;

public record TaxId : ValueObject
{
    private static readonly Regex RfcRegex = new(
        @"^[A-Z&Ñ]{3,4}[0-9]{2}(0[1-9]|1[0-2])(0[1-9]|[12][0-9]|3[01])[A-Z0-9]{2}[0-9A]$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private TaxId() { Value = null!; }

    public TaxId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("DOMAIN.INVALID_TAX_ID_FORMAT");

        var sanitizedValue = value.Trim().ToUpperInvariant();

        if (!RfcRegex.IsMatch(sanitizedValue))
            throw new DomainException("DOMAIN.INVALID_TAX_ID_FORMAT");

        Value = sanitizedValue;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(TaxId taxId) => taxId.Value;
    public static explicit operator TaxId(string taxId) => new(taxId);
}
