using Common.Domain.Entities;

namespace Product.Domain.Entities;

public sealed class Product : BaseEntity
{
    private Product() { }
    private readonly List<ProductImage> _images = new();

    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Sku { get; private set; } = default!;
    public decimal Price { get; private set; }
    public decimal? SalePrice { get; private set; }
    public string Currency { get; private set; } = "USD";
    public int StockQuantity { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public string? Brand { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Active;
    public double AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    public bool IsInStock => StockQuantity > 0 && Status == ProductStatus.Active;
    public decimal EffectivePrice => SalePrice ?? Price;

    public static Product Create(string name, string description, string sku,
        decimal price, string currency, int stockQuantity, Guid categoryId, string? brand = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required.");
        if (price <= 0) throw new ArgumentException("Price must be positive.");

        var product = new Product
        {
            Name = name, Description = description,
            Sku = sku.ToUpperInvariant(), Price = price, Currency = currency,
            StockQuantity = stockQuantity, CategoryId = categoryId, Brand = brand
        };
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Name, product.Sku));
        return product;
    }

    public void UpdateDetails(string name, string description, decimal price,
        decimal? salePrice, string? brand)
    {
        Name = name; Description = description;
        Price = price; SalePrice = salePrice; Brand = brand;
        SetUpdated("system");
    }

    public void AdjustStock(int delta)
    {
        var newQty = StockQuantity + delta;
        if (newQty < 0)
            throw new InvalidOperationException(
                $"Insufficient stock. Available: {StockQuantity}, Requested reduction: {Math.Abs(delta)}");
        StockQuantity = newQty;
        SetUpdated("system");
    }

    public void ReserveStock(int quantity)  => AdjustStock(-quantity);
    public void ReleaseStock(int quantity)  => AdjustStock(quantity);
    public void Activate()   { Status = ProductStatus.Active;   SetUpdated("system"); }
    public void Deactivate() { Status = ProductStatus.Inactive; SetUpdated("system"); }
}

public sealed class Category : BaseEntity
{
    private Category() { }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public List<Product> Products { get; private set; } = new();

    public static Category Create(string name, string? description = null, Guid? parentId = null) =>
        new()
        {
            Name = name,
            Slug = name.ToLowerInvariant().Replace(" ", "-"),
            Description = description,
            ParentCategoryId = parentId
        };
}

public sealed class ProductImage : BaseEntity
{
    private ProductImage() { }
    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = default!;
    public bool IsPrimary { get; private set; }

    public static ProductImage Create(Guid productId, string url, bool isPrimary) =>
        new() { ProductId = productId, Url = url, IsPrimary = isPrimary };
}

public enum ProductStatus { Draft, Active, Inactive, Discontinued }
