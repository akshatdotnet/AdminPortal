namespace ECommerce.Domain.ValueObjects;

/// <summary>Immutable Value Object representing a shipping/billing address.</summary>
public sealed record Address(
    string Street,
    string City,
    string State,
    string PinCode,
    string Country = "India")
{
    public string FullAddress => $"{Street}, {City}, {State} - {PinCode}, {Country}";
}
