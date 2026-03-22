using Common.Domain.Interfaces;

namespace Product.Application.DTOs;

public sealed record ProductDto(
    Guid Id, string Name, string Description, string Sku,
    decimal Price, decimal? SalePrice, decimal EffectivePrice, string Currency,
    int StockQuantity, bool IsInStock, string? Brand,
    string Status, double AverageRating, int ReviewCount,
    Guid CategoryId, string CategoryName, DateTime CreatedAt);

public sealed record ProductSummaryDto(
    Guid Id, string Name, string Sku,
    decimal EffectivePrice, string Currency,
    bool IsInStock, double AverageRating, string CategoryName);

public sealed record CategoryDto(
    Guid Id, string Name, string Slug,
    string? Description, Guid? ParentCategoryId, int ProductCount);
