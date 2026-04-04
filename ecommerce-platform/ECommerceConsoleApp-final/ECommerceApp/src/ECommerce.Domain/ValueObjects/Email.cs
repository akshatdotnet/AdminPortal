using System.Text.RegularExpressions;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Domain.ValueObjects;

/// <summary>Validated Email value object.</summary>
public sealed record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email cannot be empty.");
        if (!Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new DomainException($"'{value}' is not a valid email.");
        Value = value.ToLowerInvariant();
    }

    public override string ToString() => Value;
}
