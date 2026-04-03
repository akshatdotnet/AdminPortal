using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Cart line-item. Persisted as plain scalar columns (no EF-owned type) to avoid
/// EF Core change-tracker conflicts when the cart collection is mutated across operations.
/// UnitPrice is exposed as a computed Money value object built from the scalar columns.
/// </summary>
public class CartItem : Entity
{
    public Guid    Id          { get; private set; }
    public Guid    CartId      { get; private set; }
    public Guid    ProductId   { get; private set; }
    public string  ProductName { get; private set; } = string.Empty;

    // Stored as plain scalars — no OwnsOne, no EF owned-entity tracking issues
    public decimal UnitPriceAmount   { get; private set; }
    public string  UnitPriceCurrency { get; private set; } = "INR";
    public int     Quantity          { get; private set; }

    // Computed projections (not mapped to DB columns)
    public Money UnitPrice => new(UnitPriceAmount, UnitPriceCurrency);
    public Money Subtotal  => new(UnitPriceAmount * Quantity, UnitPriceCurrency);

    private CartItem() { }

    public static CartItem Create(
        Guid cartId, Guid productId, string productName, Money unitPrice, int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive.");

        return new CartItem
        {
            Id                = Guid.NewGuid(),
            CartId            = cartId,
            ProductId         = productId,
            ProductName       = productName,
            UnitPriceAmount   = unitPrice.Amount,
            UnitPriceCurrency = unitPrice.Currency,
            Quantity          = quantity
        };
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new DomainException("Quantity must be positive.");
        Quantity = newQuantity;
    }
}
