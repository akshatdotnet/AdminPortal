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

    public static Product Create(
        string name, string description, string sku,
        decimal price, string currency, int stockQuantity,
        Guid categoryId, string? brand = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.");
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.");
        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.");

        var product = new Product
        {
            Name        = name.Trim(),
            Description = description,
            Sku         = sku.Trim().ToUpperInvariant(),
            Price       = price,
            Currency    = currency.ToUpperInvariant(),
            StockQuantity = stockQuantity,
            CategoryId  = categoryId,
            Brand       = brand?.Trim()
        };
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Name, product.Sku));
        return product;
    }

    public void UpdateDetails(
        string name, string description, decimal price,
        decimal? salePrice, string? brand, Guid categoryId, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.");
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.");
        if (salePrice.HasValue && salePrice.Value >= price)
            throw new ArgumentException("Sale price must be less than the regular price.");

        Name        = name.Trim();
        Description = description;
        Price       = price;
        SalePrice   = salePrice;
        Brand       = brand?.Trim();
        CategoryId  = categoryId;
        SetUpdated(updatedBy);
    }

    public void AdjustStock(int delta, string updatedBy = "system")
    {
        var newQty = StockQuantity + delta;
        if (newQty < 0)
            throw new InvalidOperationException(
                $"Insufficient stock. Current: {StockQuantity}, requested change: {delta}.");
        StockQuantity = newQty;

        if (newQty == 0)
            AddDomainEvent(new ProductOutOfStockEvent(Id, Name));
        else if (newQty <= 5)
            AddDomainEvent(new ProductLowStockEvent(Id, Name, newQty));

        SetUpdated(updatedBy);
    }

    public void ReserveStock(int quantity)  => AdjustStock(-quantity);
    public void ReleaseStock(int quantity)  => AdjustStock(quantity);

    public void Activate(string updatedBy)
    {
        Status = ProductStatus.Active;
        SetUpdated(updatedBy);
    }

    public void Deactivate(string updatedBy)
    {
        Status = ProductStatus.Inactive;
        SetUpdated(updatedBy);
    }

    public void Delete(string deletedBy) => SoftDelete(deletedBy);
}

public sealed class Category : BaseEntity
{
    private Category() { }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public List<Product> Products { get; private set; } = new();

    public static Category Create(
        string name, string? description = null, Guid? parentId = null) =>
        new()
        {
            Name              = name.Trim(),
            Slug              = name.Trim().ToLowerInvariant().Replace(" ", "-"),
            Description       = description?.Trim(),
            ParentCategoryId  = parentId
        };

    public void Update(string name, string? description)
    {
        Name        = name.Trim();
        Slug        = name.Trim().ToLowerInvariant().Replace(" ", "-");
        Description = description?.Trim();
        SetUpdated("admin");
    }
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

public enum ProductStatus { Draft = 0, Active = 1, Inactive = 2, Discontinued = 3 }
