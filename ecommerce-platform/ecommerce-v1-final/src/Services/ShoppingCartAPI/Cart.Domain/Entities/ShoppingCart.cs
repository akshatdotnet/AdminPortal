namespace Cart.Domain.Entities;

public sealed class ShoppingCart
{
    private readonly List<CartItem> _items = new();

    public Guid CustomerId { get; private set; }
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();
    public string? AppliedCouponCode { get; private set; }
    public decimal CouponDiscount { get; private set; }
    public decimal Subtotal => _items.Sum(i => i.LineTotal);
    public decimal Total => Math.Max(0, Subtotal - CouponDiscount);
    public int ItemCount => _items.Sum(i => i.Quantity);
    public DateTime LastModified { get; private set; } = DateTime.UtcNow;

    public static ShoppingCart Create(Guid customerId) => new() { CustomerId = customerId };

    public void AddItem(Guid productId, string productName, string sku,
        decimal unitPrice, int quantity, string? imageUrl = null)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.");
        if (unitPrice <= 0) throw new ArgumentException("Price must be positive.");

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null) existing.UpdateQuantity(existing.Quantity + quantity);
        else _items.Add(new CartItem
        {
            ProductId = productId, ProductName = productName, Sku = sku,
            UnitPrice = unitPrice, Quantity = quantity, ImageUrl = imageUrl
        });
        Touch();
    }

    public void UpdateItemQuantity(Guid productId, int newQty)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new InvalidOperationException($"Product {productId} not in cart.");
        if (newQty <= 0) _items.Remove(item);
        else item.UpdateQuantity(newQty);
        Touch();
    }

    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is not null) { _items.Remove(item); Touch(); }
    }

    public void ApplyCoupon(string code, decimal discount)
    {
        AppliedCouponCode = code; CouponDiscount = discount; Touch();
    }

    public void RemoveCoupon() { AppliedCouponCode = null; CouponDiscount = 0; Touch(); }

    public void Clear() { _items.Clear(); RemoveCoupon(); }

    private void Touch() => LastModified = DateTime.UtcNow;
}

public sealed class CartItem
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public string Sku { get; init; } = default!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public string? ImageUrl { get; init; }
    public decimal LineTotal => UnitPrice * Quantity;

    internal void UpdateQuantity(int qty) => Quantity = qty;
    internal void UpdatePrice(decimal price) => UnitPrice = price;
}
