using Microsoft.EntityFrameworkCore;
using Zovo.Core.Entities;
using Zovo.Core.Interfaces;
using Zovo.Core.ValueObjects;

namespace Zovo.Application.Products;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    public ProductService(IUnitOfWork uow) => _uow = uow;

    public async Task<PagedResult<ProductListItemDto>> GetPagedAsync(ProductQueryParams q)
    {
        var query = _uow.Products.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(p => p.Name.Contains(q.Search) ||
                (p.SKU != null && p.SKU.Contains(q.Search)));

        if (!string.IsNullOrWhiteSpace(q.Category) && q.Category != "all")
            query = query.Where(p => p.Category == q.Category);

        query = q.Status switch {
            "active"   => query.Where(p => p.IsActive),
            "inactive" => query.Where(p => !p.IsActive),
            "low"      => query.Where(p => p.Stock > 0 && p.Stock <= p.LowStockThreshold),
            "out"      => query.Where(p => p.Stock == 0),
            _          => query
        };

        query = q.SortBy switch {
            "name_desc"  => query.OrderByDescending(p => p.Name),
            "price_asc"  => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "stock_asc"  => query.OrderBy(p => p.Stock),
            "stock_desc" => query.OrderByDescending(p => p.Stock),
            "newest"     => query.OrderByDescending(p => p.CreatedAt),
            _            => query.OrderBy(p => p.Name)
        };

        var total = await query.CountAsync();
        var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync();
        return PagedResult<ProductListItemDto>.Create(items.Select(ToListItem), total, q.Page, q.PageSize);
    }

    public async Task<ProductDetailDto?> GetDetailAsync(int id)
    {
        var p = await _uow.Products.GetByIdAsync(id);
        return p is null ? null : ToDetail(p);
    }

    public async Task<Result<int>> CreateAsync(CreateProductCommand cmd)
    {
        var p = new Product {
            Name = cmd.Name, SKU = cmd.SKU, Category = cmd.Category,
            SubCategory = cmd.SubCategory, Price = cmd.Price,
            CompareAtPrice = cmd.CompareAtPrice, CostPrice = cmd.CostPrice,
            Stock = cmd.Stock, LowStockThreshold = cmd.LowStockThreshold,
            IsActive = cmd.IsActive, IsFeatured = cmd.IsFeatured,
            ImageUrl = cmd.ImageUrl, Description = cmd.Description,
            Tags = cmd.Tags, Weight = cmd.Weight,
            Slug = Slugify(cmd.Name)
        };
        await _uow.Products.AddAsync(p);
        await _uow.SaveChangesAsync();
        return Result<int>.Ok(p.Id, $"Product '{p.Name}' created successfully.");
    }

    public async Task<Result> UpdateAsync(UpdateProductCommand cmd)
    {
        var p = await _uow.Products.GetByIdAsync(cmd.Id);
        if (p is null) return Result.Fail("Product not found.", "NOT_FOUND");
        p.Name = cmd.Name; p.SKU = cmd.SKU; p.Category = cmd.Category;
        p.SubCategory = cmd.SubCategory; p.Price = cmd.Price;
        p.CompareAtPrice = cmd.CompareAtPrice; p.CostPrice = cmd.CostPrice;
        p.Stock = cmd.Stock; p.LowStockThreshold = cmd.LowStockThreshold;
        p.IsActive = cmd.IsActive; p.IsFeatured = cmd.IsFeatured;
        p.ImageUrl = cmd.ImageUrl; p.Description = cmd.Description;
        p.Tags = cmd.Tags; p.Weight = cmd.Weight;
        await _uow.Products.UpdateAsync(p);
        await _uow.SaveChangesAsync();
        return Result.Ok($"Product '{p.Name}' updated successfully.");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var p = await _uow.Products.GetByIdAsync(id);
        if (p is null) return Result.Fail("Product not found.", "NOT_FOUND");
        await _uow.Products.DeleteAsync(p);
        await _uow.SaveChangesAsync();
        return Result.Ok($"Product '{p.Name}' deleted.");
    }

    public async Task<Result> ToggleStatusAsync(int id)
    {
        var p = await _uow.Products.GetByIdAsync(id);
        if (p is null) return Result.Fail("Product not found.", "NOT_FOUND");
        p.IsActive = !p.IsActive;
        await _uow.Products.UpdateAsync(p);
        await _uow.SaveChangesAsync();
        return Result.Ok($"Product '{p.Name}' {(p.IsActive ? "activated" : "deactivated")}.");
    }

    public Task<IEnumerable<string>> GetCategoriesAsync() => _uow.Products.GetCategoriesAsync();

    private static string Slugify(string name)
        => System.Text.RegularExpressions.Regex
            .Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');

    private static ProductListItemDto ToListItem(Product p)
    {
        var level   = p.Stock == 0 ? "out" : p.Stock <= p.LowStockThreshold ? "low"
                    : p.Stock <= p.LowStockThreshold * 3 ? "medium" : "high";
        var display = p.Stock == 0 ? "Out of stock" : $"{p.Stock:N0} units";
        return new(p.Id, p.Name, p.SKU, p.Category, p.Price, p.Stock,
            p.IsActive, p.IsFeatured, p.ImageUrl, display, level);
    }

    private static ProductDetailDto ToDetail(Product p) => new(
        p.Id, p.Name, p.SKU, p.Slug, p.Category, p.SubCategory,
        p.Price, p.CompareAtPrice, p.CostPrice, p.Stock, p.LowStockThreshold,
        p.IsActive, p.IsFeatured, p.ImageUrl, p.Description, p.Tags,
        p.Weight, p.CreatedAt, p.UpdatedAt);
}
