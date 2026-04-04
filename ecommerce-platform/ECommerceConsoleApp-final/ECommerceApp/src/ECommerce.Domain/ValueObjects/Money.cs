namespace ECommerce.Domain.ValueObjects;

/// <summary>
/// Immutable Value Object representing an amount with currency.
/// Two Money instances are equal when both amount and currency match.
/// </summary>
public sealed record Money(decimal Amount, string Currency = "INR")
{
    public static readonly Money Zero = new(0);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} to {other.Currency}");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity) => new(Amount * quantity, Currency);

    public static Money operator +(Money a, Money b) => a.Add(b);

    public override string ToString() => $"{Currency} {Amount:N2}";
}
