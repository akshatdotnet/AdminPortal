using ECommerce.Domain.Exceptions;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

/// <summary>Cart Aggregate Root.</summary>
public class Cart : Entity
{
    private readonly List<CartItem> _items = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Money TotalAmount => _items.Aggregate(Money.Zero, (sum, i) => sum + i.Subtotal);
    public int TotalItems   => _items.Sum(i => i.Quantity);

    private Cart() { }

    public static Cart Create(Guid customerId) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        CreatedAt  = DateTime.UtcNow,
        UpdatedAt  = DateTime.UtcNow
    };

    /// <summary>
    /// Called by the repository after loading to populate the items collection.
    /// Necessary because EF no longer manages Cart.Items as a navigation property.
    /// </summary>
    public void LoadItems(IEnumerable<CartItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }

    public void AddItem(Guid productId, string productName, Money unitPrice, int quantity)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing != null)
            existing.UpdateQuantity(existing.Quantity + quantity);
        else
        {
            if (_items.Count >= 20)
                throw new DomainException("Cart cannot have more than 20 distinct products.");
            _items.Add(CartItem.Create(Id, productId, productName, unitPrice, quantity));
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new DomainException("Item not found in cart.");
        _items.Remove(item);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Clear()
    {
        _items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Updates UpdatedAt — used when items are managed via CartItemRepository.</summary>
    public void Touch() => UpdatedAt = DateTime.UtcNow;
}
