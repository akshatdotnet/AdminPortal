namespace Product.Application.DTOs;

public sealed record ProductDto(
    Guid   Id,
    string Name,
    string Description,
    string Sku,
    decimal  Price,
    decimal? SalePrice,
    decimal  EffectivePrice,
    string   Currency,
    int    StockQuantity,
    bool   IsInStock,
    string? Brand,
    string  Status,
    double  AverageRating,
    int     ReviewCount,
    Guid   CategoryId,
    string CategoryName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record ProductSummaryDto(
    Guid   Id,
    string Name,
    string Sku,
    decimal EffectivePrice,
    string  Currency,
    int     StockQuantity,
    bool    IsInStock,
    double  AverageRating,
    string  CategoryName,
    string  Status);

public sealed record CategoryDto(
    Guid   Id,
    string Name,
    string Slug,
    string? Description,
    Guid?  ParentCategoryId,
    int    ProductCount);

public sealed record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage)
{
    public static PagedResult<T> Create(
        IEnumerable<T> items, int total, int page, int size)
    {
        var totalPages = (int)Math.Ceiling((double)total / size);
        return new PagedResult<T>(
            items, total, page, size, totalPages,
            page > 1, page < totalPages);
    }
}
