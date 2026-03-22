using Common.Domain.Entities;

namespace Product.Domain.Entities;

// ══════════════════════════════════════════════════════════════
// PRODUCT AGGREGATE ROOT
// ══════════════════════════════════════════════════════════════
public sealed class Product : BaseEntity
{
    private Product() { }

    private readonly List<ProductImage> _images = [];
    private readonly List<ProductReview> _reviews = [];

    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Sku { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public Money? SalePrice { get; private set; }
    public int StockQuantity { get; private set; }
    public int LowStockThreshold { get; private set; } = 10;
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = default!;
    public string? Brand { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Draft;
    public double AverageRating { get; private set; }
    public int ReviewCount { get; private set; }

    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<ProductReview> Reviews => _reviews.AsReadOnly();

    public bool IsInStock => StockQuantity > 0 && Status == ProductStatus.Active;
    public bool IsLowStock => StockQuantity <= LowStockThreshold && StockQuantity > 0;
    public Money EffectivePrice => SalePrice ?? Price;

    // ── Factory ────────────────────────────────────────────────
    public static Product Create(
        string name, string description, string sku,
        Money price, int stockQuantity, Guid categoryId,
        string? brand = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        var product = new Product
        {
            Name = name,
            Description = description,
            Sku = sku.ToUpperInvariant(),
            Price = price,
            StockQuantity = stockQuantity,
            CategoryId = categoryId,
            Brand = brand
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Name, product.Sku));
        return product;
    }

    // ── Behaviour ─────────────────────────────────────────────
    public void UpdatePrice(Money newPrice, Money? salePrice = null)
    {
        Price = newPrice;
        SalePrice = salePrice;
        SetUpdated("system");
    }

    public void AdjustStock(int delta, string reason)
    {
        var newQty = StockQuantity + delta;
        if (newQty < 0)
            throw new InvalidOperationException(
                $"Insufficient stock. Available: {StockQuantity}, Requested: {Math.Abs(delta)}");

        StockQuantity = newQty;

        if (IsLowStock)
            AddDomainEvent(new ProductLowStockEvent(Id, Name, StockQuantity));

        if (StockQuantity == 0)
            AddDomainEvent(new ProductOutOfStockEvent(Id, Name));

        SetUpdated("system");
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        AdjustStock(-quantity, "reservation");
    }

    public void ReleaseStock(int quantity) =>
        AdjustStock(quantity, "reservation-release");

    public void Activate()
    {
        if (string.IsNullOrEmpty(Name) || Price.Amount <= 0)
            throw new InvalidOperationException("Product must have name and price before activation.");

        Status = ProductStatus.Active;
        SetUpdated("system");
    }

    public void Deactivate()
    {
        Status = ProductStatus.Inactive;
        SetUpdated("system");
    }

    public void AddImage(string url, bool isPrimary = false)
    {
        if (isPrimary) _images.ForEach(i => i.SetPrimary(false));
        _images.Add(ProductImage.Create(Id, url, isPrimary));
    }

    public void AddReview(Guid userId, int rating, string comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be 1-5.");

        if (_reviews.Any(r => r.UserId == userId))
            throw new InvalidOperationException("User has already reviewed this product.");

        _reviews.Add(ProductReview.Create(Id, userId, rating, comment));
        RecalculateRating();
    }

    private void RecalculateRating()
    {
        ReviewCount = _reviews.Count;
        AverageRating = ReviewCount > 0
            ? Math.Round(_reviews.Average(r => r.Rating), 1)
            : 0;
    }
}

// ══════════════════════════════════════════════════════════════
// CATEGORY ENTITY
// ══════════════════════════════════════════════════════════════
public sealed class Category : BaseEntity
{
    private Category() { }

    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public Category? ParentCategory { get; private set; }
    public List<Category> SubCategories { get; private set; } = [];
    public List<Product> Products { get; private set; } = [];

    public static Category Create(string name, string? description = null, Guid? parentId = null) =>
        new()
        {
            Name = name,
            Slug = name.ToLowerInvariant().Replace(" ", "-"),
            Description = description,
            ParentCategoryId = parentId
        };
}

// ══════════════════════════════════════════════════════════════
// PRODUCT IMAGE ENTITY
// ══════════════════════════════════════════════════════════════
public sealed class ProductImage : BaseEntity
{
    private ProductImage() { }

    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = default!;
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }

    internal static ProductImage Create(Guid productId, string url, bool isPrimary) =>
        new() { ProductId = productId, Url = url, IsPrimary = isPrimary };

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}

// ══════════════════════════════════════════════════════════════
// PRODUCT REVIEW ENTITY
// ══════════════════════════════════════════════════════════════
public sealed class ProductReview : BaseEntity
{
    private ProductReview() { }

    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    public int Rating { get; private set; }
    public string Comment { get; private set; } = default!;
    public bool IsVerifiedPurchase { get; private set; }

    internal static ProductReview Create(Guid productId, Guid userId, int rating, string comment) =>
        new()
        {
            ProductId = productId,
            UserId = userId,
            Rating = rating,
            Comment = comment
        };
}

// ══════════════════════════════════════════════════════════════
// VALUE OBJECTS
// ══════════════════════════════════════════════════════════════
public sealed record Money(decimal Amount, string Currency = "USD")
{
    public static readonly Money Zero = new(0);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency) throw new InvalidOperationException("Currency mismatch.");
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator *(Money m, int multiplier) =>
        new(m.Amount * multiplier, m.Currency);

    public static Money operator *(Money m, decimal multiplier) =>
        new(Math.Round(m.Amount * multiplier, 2), m.Currency);
}

public enum ProductStatus { Draft, Active, Inactive, Discontinued }
