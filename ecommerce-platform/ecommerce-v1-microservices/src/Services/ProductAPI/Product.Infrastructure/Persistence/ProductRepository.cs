using Microsoft.EntityFrameworkCore;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using Product.Domain.Entities;
using ProductEntity   = Product.Domain.Entities.Product;
using CategoryEntity  = Product.Domain.Entities.Category;
using ProductImageEntity = Product.Domain.Entities.ProductImage;

namespace Product.Infrastructure.Persistence;

public sealed class ProductRepository(ProductDbContext ctx) : IProductRepository
{
    public async Task<Product.Domain.Entities.Product?> GetByIdAsync(
        Guid id, CancellationToken ct = default)
        => await ctx.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> SkuExistsAsync(
        string sku, CancellationToken ct = default)
        => await ctx.Products
            .AnyAsync(p => p.Sku == sku.Trim().ToUpperInvariant(), ct);

    public void Add(Product.Domain.Entities.Product p)    => ctx.Products.Add(p);
    public void Update(Product.Domain.Entities.Product p) => ctx.Products.Update(p);
    public void Delete(Product.Domain.Entities.Product p) => ctx.Products.Update(p);
}

public sealed class CategoryRepository(ProductDbContext ctx) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(
        Guid id, CancellationToken ct = default)
        => await ctx.Categories.FindAsync(new object[] { id }, ct);

    public async Task<bool> SlugExistsAsync(
        string slug, CancellationToken ct = default)
        => await ctx.Categories.AnyAsync(c => c.Slug == slug, ct);

    public async Task<IEnumerable<Category>> GetAllAsync(
        CancellationToken ct = default)
        => await ctx.Categories.ToListAsync(ct);

    public void Add(Category cat) => ctx.Categories.Add(cat);
}

public sealed class ProductReadRepository(ProductDbContext ctx) : IProductReadRepository
{
    public async Task<ProductDto?> GetByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        var p = await ctx.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? null : ToDto(p);
    }

    public async Task<PagedResult<ProductSummaryDto>> GetPagedAsync(
        int page, int size,
        string? search, Guid? categoryId,
        decimal? minPrice, decimal? maxPrice,
        bool inStockOnly, string? status,
        string? sortBy, bool sortDesc,
        CancellationToken ct = default)
    {
        var q = ctx.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        // Filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(s) || p.Sku.Contains(s));
        }
        if (categoryId.HasValue)
            q = q.Where(p => p.CategoryId == categoryId.Value);
        if (minPrice.HasValue)
            q = q.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            q = q.Where(p => p.Price <= maxPrice.Value);
        if (inStockOnly)
            q = q.Where(p => p.StockQuantity > 0);
        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<ProductStatus>(status, ignoreCase: true, out var st))
            q = q.Where(p => p.Status == st);
        else
            q = q.Where(p => p.Status == ProductStatus.Active);

        // Sorting
        q = (sortBy?.ToLower(), sortDesc) switch
        {
            ("price",  false) => q.OrderBy(p => p.Price),
            ("price",  true)  => q.OrderByDescending(p => p.Price),
            ("rating", false) => q.OrderBy(p => p.AverageRating),
            ("rating", true)  => q.OrderByDescending(p => p.AverageRating),
            ("stock",  false) => q.OrderBy(p => p.StockQuantity),
            ("stock",  true)  => q.OrderByDescending(p => p.StockQuantity),
            _                 => q.OrderBy(p => p.Name)
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return PagedResult<ProductSummaryDto>.Create(
            items.Select(ToSummary), total, page, size);
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(
        CancellationToken ct = default)
    {
        var cats = await ctx.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return cats.Select(c => new CategoryDto(
            c.Id, c.Name, c.Slug, c.Description,
            c.ParentCategoryId,
            c.Products.Count(p => !p.IsDeleted)));
    }

    private static ProductDto ToDto(Product.Domain.Entities.Product p) =>
        new(p.Id, p.Name, p.Description, p.Sku,
            p.Price, p.SalePrice, p.EffectivePrice, p.Currency,
            p.StockQuantity, p.IsInStock, p.Brand,
            p.Status.ToString(), p.AverageRating, p.ReviewCount,
            p.CategoryId, p.Category?.Name ?? string.Empty,
            p.CreatedAt, p.UpdatedAt);

    private static ProductSummaryDto ToSummary(Product.Domain.Entities.Product p) =>
        new(p.Id, p.Name, p.Sku,
            p.EffectivePrice, p.Currency,
            p.StockQuantity, p.IsInStock,
            p.AverageRating, p.Category?.Name ?? string.Empty,
            p.Status.ToString());
}

public sealed class UnitOfWorkProduct(ProductDbContext ctx) : IUnitOfWorkProduct
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => ctx.SaveChangesAsync(ct);
}
