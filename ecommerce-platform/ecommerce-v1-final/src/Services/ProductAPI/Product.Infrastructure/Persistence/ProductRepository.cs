using Common.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using Product.Domain.Entities;

namespace Product.Infrastructure.Persistence;

public sealed class ProductRepository(ProductDbContext ctx) : IProductRepository
{
    public async Task<Product.Domain.Entities.Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await ctx.Products.Include(p => p.Images).Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default)
        => await ctx.Products.AnyAsync(p => p.Sku == sku.ToUpperInvariant(), ct);

    public void Add(Product.Domain.Entities.Product p)    => ctx.Products.Add(p);
    public void Update(Product.Domain.Entities.Product p) => ctx.Products.Update(p);
}

public sealed class CategoryRepository(ProductDbContext ctx) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await ctx.Categories.FindAsync(new object[] { id }, ct);
    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken ct = default)
        => await ctx.Categories.ToListAsync(ct);
    public void Add(Category cat) => ctx.Categories.Add(cat);
}

public sealed class ProductReadRepository(ProductDbContext ctx) : IProductReadRepository
{
    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var p = await ctx.Products.AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? null : ToDto(p);
    }

    public async Task<PagedResult<ProductSummaryDto>> GetPagedAsync(
        int page, int size, string? search, Guid? categoryId,
        decimal? minPrice, decimal? maxPrice, bool inStockOnly, CancellationToken ct = default)
    {
        var q = ctx.Products.AsNoTracking().Include(p => p.Category)
            .Where(p => p.Status == ProductStatus.Active);

        if (!string.IsNullOrEmpty(search))
            q = q.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        if (categoryId.HasValue)
            q = q.Where(p => p.CategoryId == categoryId.Value);
        if (minPrice.HasValue)
            q = q.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            q = q.Where(p => p.Price <= maxPrice.Value);
        if (inStockOnly)
            q = q.Where(p => p.StockQuantity > 0);

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(p => p.Name)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);

        return PagedResult<ProductSummaryDto>.Create(items.Select(ToSummary), total, page, size);
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var cats = await ctx.Categories.AsNoTracking()
            .Include(c => c.Products).ToListAsync(ct);
        return cats.Select(c => new CategoryDto(
            c.Id, c.Name, c.Slug, c.Description,
            c.ParentCategoryId, c.Products.Count));
    }

    private static ProductDto ToDto(Product.Domain.Entities.Product p) => new(
        p.Id, p.Name, p.Description, p.Sku,
        p.Price, p.SalePrice, p.EffectivePrice, p.Currency,
        p.StockQuantity, p.IsInStock, p.Brand,
        p.Status.ToString(), p.AverageRating, p.ReviewCount,
        p.CategoryId, p.Category?.Name ?? "", p.CreatedAt);

    private static ProductSummaryDto ToSummary(Product.Domain.Entities.Product p) => new(
        p.Id, p.Name, p.Sku, p.EffectivePrice, p.Currency,
        p.IsInStock, p.AverageRating, p.Category?.Name ?? "");
}

public sealed class UnitOfWorkProduct(ProductDbContext ctx) : IUnitOfWorkProduct
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        ctx.SaveChangesAsync(ct);
}
