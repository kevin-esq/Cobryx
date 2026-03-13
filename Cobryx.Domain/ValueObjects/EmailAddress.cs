using System.Text.RegularExpressions;

using Cobryx.Domain.Shared;

namespace Cobryx.Domain.ValueObjects;

public partial record EmailAddress : ValueObject
{
    private static readonly Regex EmailRegex = MyRegex();

    private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com", "guerrillamail.com", "10minutemail.com", "temp-mail.org", "yopmail.com"
    };

    public string Value { get; }

    private EmailAddress() { Value = null!; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(DomainErrorCode.ValueObjects.InvalidEmailFormat);

        var trimmedValue = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(trimmedValue))
            throw new DomainException(DomainErrorCode.ValueObjects.InvalidEmailFormat);

        var domain = trimmedValue.Split('@')[1];
        if (DisposableDomains.Contains(domain))
            throw new DomainException(DomainErrorCode.ValueObjects.DisposableEmail);

        Value = trimmedValue;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(EmailAddress email) => email.Value;
    public static explicit operator EmailAddress(string email) => new(email);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex MyRegex();
}
