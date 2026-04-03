using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Product Aggregate Root.
/// Encapsulates stock management and pricing rules.
/// </summary>
public class Product : Entity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = Money.Zero;
    public int StockQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public Guid CategoryId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public int AvailableQuantity => StockQuantity - ReservedQuantity;

    private Product() { }

    public static Product Create(string name, string description, decimal price, int stock, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name cannot be empty.");
        if (price < 0)
            throw new DomainException("Price cannot be negative.");
        if (stock < 0)
            throw new DomainException("Stock cannot be negative.");

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = new Money(price),
            StockQuantity = stock,
            ReservedQuantity = 0,
            CategoryId = categoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Reserve quantity must be positive.");
        if (AvailableQuantity < quantity)
            throw new DomainException($"Insufficient stock. Available: {AvailableQuantity}, Requested: {quantity}");
        ReservedQuantity += quantity;
    }

    public void ReleaseReservation(int quantity)
    {
        ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
    }

    public void ConfirmSale(int quantity)
    {
        if (StockQuantity < quantity)
            throw new DomainException("Cannot confirm sale: insufficient stock.");
        StockQuantity -= quantity;
        ReservedQuantity = Math.Max(0, ReservedQuantity - quantity);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void UpdatePrice(decimal newPrice) => Price = new Money(newPrice);
    public void AddStock(int quantity) => StockQuantity += quantity;
}
