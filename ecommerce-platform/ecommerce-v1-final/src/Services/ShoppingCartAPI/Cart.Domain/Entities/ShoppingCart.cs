namespace Cart.Domain.Entities;

public sealed class ShoppingCart
{
    private readonly List<CartItem> _items = new();

    public Guid   CustomerId       { get; private set; }
    public string? AppliedCouponCode { get; private set; }
    public decimal CouponDiscount  { get; private set; }
    public decimal Subtotal        => _items.Sum(i => i.LineTotal);
    public decimal Total           => Math.Max(0, Subtotal - CouponDiscount);
    public int     ItemCount       => _items.Sum(i => i.Quantity);
    public DateTime LastModified   { get; private set; } = DateTime.UtcNow;

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public static ShoppingCart Create(Guid customerId) =>
        new() { CustomerId = customerId };

    public void AddItem(
        Guid productId, string productName, string sku,
        decimal unitPrice, int quantity, string? imageUrl = null)
    {
        if (quantity  <= 0) throw new ArgumentException("Quantity must be positive.");
        if (unitPrice <= 0) throw new ArgumentException("Price must be positive.");

        var existing = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is not null)
        {
            existing.UpdateQuantity(existing.Quantity + quantity);
        }
        else
        {
            // Use the factory method — avoids calling private setters
            _items.Add(CartItem.Create(
                productId, productName, sku, unitPrice, quantity, imageUrl));
        }
        Touch();
    }

    public void UpdateItemQuantity(Guid productId, int newQty)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId)
            ?? throw new InvalidOperationException(
                $"Product {productId} is not in the cart.");
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
        AppliedCouponCode = code;
        CouponDiscount    = discount;
        Touch();
    }

    public void RemoveCoupon()
    {
        AppliedCouponCode = null;
        CouponDiscount    = 0;
        Touch();
    }

    public void Clear() { _items.Clear(); RemoveCoupon(); }

    private void Touch() => LastModified = DateTime.UtcNow;
}

public sealed class CartItem
{
    // Private constructor — use the factory method
    private CartItem() { }

    public Guid    ProductId   { get; private set; }
    public string  ProductName { get; private set; } = default!;
    public string  Sku         { get; private set; } = default!;
    public decimal UnitPrice   { get; private set; }
    public int     Quantity    { get; private set; }
    public string? ImageUrl    { get; private set; }
    public decimal LineTotal   => UnitPrice * Quantity;

    // ── Factory method — the only way to create a CartItem ───
    internal static CartItem Create(
        Guid productId, string productName, string sku,
        decimal unitPrice, int quantity, string? imageUrl) =>
        new()
        {
            ProductId   = productId,
            ProductName = productName,
            Sku         = sku,
            UnitPrice   = unitPrice,
            Quantity    = quantity,
            ImageUrl    = imageUrl
        };

    // ── Mutation methods — internal so only ShoppingCart can call them ──
    internal void UpdateQuantity(int qty)   => Quantity  = qty;
    internal void UpdatePrice(decimal price) => UnitPrice = price;
}
