using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.ValueObjects;

public record EmailAddress : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com", "guerrillamail.com", "10minutemail.com", "temp-mail.org", "yopmail.com"
    };

    public string Value { get; }

    private EmailAddress() { Value = null!; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email address cannot be empty.", nameof(value));

        var trimmedValue = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(trimmedValue))
            throw new ArgumentException("Invalid email format.", nameof(value));

        var domain = trimmedValue.Split('@')[1];
        if (DisposableDomains.Contains(domain))
            throw new ArgumentException("Disposable email domains are not allowed.", nameof(value));

        Value = trimmedValue;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(EmailAddress email) => email.Value;
    public static explicit operator EmailAddress(string email) => new(email);
}
