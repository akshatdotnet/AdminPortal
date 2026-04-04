using System.ComponentModel.DataAnnotations;
using Zovo.Core.ValueObjects;

namespace Zovo.Application.Products;

// ── Output DTOs ───────────────────────────────────────────────
public record ProductListItemDto(
    int Id, string Name, string? SKU, string Category,
    decimal Price, int Stock, bool IsActive, bool IsFeatured,
    string? ImageUrl, string StockDisplay, string StockLevel);

public record ProductDetailDto(
    int Id, string Name, string? SKU, string? Slug, string Category,
    string? SubCategory, decimal Price, decimal? CompareAtPrice,
    decimal? CostPrice, int Stock, int LowStockThreshold,
    bool IsActive, bool IsFeatured, string? ImageUrl,
    string? Description, string? Tags, decimal Weight,
    DateTime CreatedAt, DateTime UpdatedAt);

// ── Input Commands ────────────────────────────────────────────
public class CreateProductCommand
{
    [Required, StringLength(200)] public string Name          { get; set; } = "";
    [StringLength(50)]            public string? SKU          { get; set; }
    [Required, StringLength(100)] public string Category      { get; set; } = "";
    [StringLength(100)]           public string? SubCategory  { get; set; }
    [Required, Range(0, 9_999_999)] public decimal Price      { get; set; }
    [Range(0, 9_999_999)]         public decimal? CompareAtPrice { get; set; }
    [Range(0, 9_999_999)]         public decimal? CostPrice   { get; set; }
    [Required, Range(0, 999_999)] public int Stock            { get; set; }
    [Range(1, 1000)]              public int LowStockThreshold { get; set; } = 10;
    public bool IsActive    { get; set; } = true;
    public bool IsFeatured  { get; set; }
    [Url] public string? ImageUrl { get; set; }
    [StringLength(2000)] public string? Description { get; set; }
    [StringLength(500)]  public string? Tags        { get; set; }
    [Range(0, 100)]      public decimal Weight      { get; set; }
}

public class UpdateProductCommand : CreateProductCommand
{
    public int Id { get; set; }
}

// ── Query params ──────────────────────────────────────────────
public class ProductQueryParams
{
    public string? Search   { get; set; }
    public string? Category { get; set; }
    public string? Status   { get; set; }   // active|inactive|low|out
    public string SortBy    { get; set; } = "name_asc";
    public int Page         { get; set; } = 1;
    public int PageSize     { get; set; } = 20;
}

// ── Service contract ──────────────────────────────────────────
public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> GetPagedAsync(ProductQueryParams q);
    Task<ProductDetailDto?> GetDetailAsync(int id);
    Task<Result<int>> CreateAsync(CreateProductCommand cmd);
    Task<Result> UpdateAsync(UpdateProductCommand cmd);
    Task<Result> DeleteAsync(int id);
    Task<Result> ToggleStatusAsync(int id);
    Task<IEnumerable<string>> GetCategoriesAsync();
}
