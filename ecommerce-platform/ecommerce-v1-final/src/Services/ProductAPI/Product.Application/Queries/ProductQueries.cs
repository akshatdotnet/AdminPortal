using Common.Domain.Primitives;
using MediatR;
using Product.Application.DTOs;
using Product.Application.Interfaces;
using ProductEntity   = Product.Domain.Entities.Product;
using CategoryEntity  = Product.Domain.Entities.Category;
using ProductImageEntity = Product.Domain.Entities.ProductImage;

namespace Product.Application.Queries;

// ── Get single product ────────────────────────────────────────
public sealed record GetProductByIdQuery(Guid ProductId)
    : IRequest<Result<ProductDto>>;

public sealed class GetProductByIdQueryHandler(IProductReadRepository repo)
    : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(
        GetProductByIdQuery q, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(q.ProductId, ct);
        return p is null
            ? Result.Failure<ProductDto>(Error.NotFound("ProductEntity", q.ProductId))
            : Result.Success(p);
    }
}

// ── Get paged product list ────────────────────────────────────
public sealed record GetProductsQuery(
    int     PageNumber  = 1,
    int     PageSize    = 20,
    string? Search      = null,
    Guid?   CategoryId  = null,
    decimal? MinPrice   = null,
    decimal? MaxPrice   = null,
    bool    InStockOnly = false,
    string? Status      = null,
    string? SortBy      = null,
    bool    SortDesc    = false)
    : IRequest<Result<PagedResult<ProductSummaryDto>>>;

public sealed class GetProductsQueryHandler(IProductReadRepository repo)
    : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductSummaryDto>>>
{
    public async Task<Result<PagedResult<ProductSummaryDto>>> Handle(
        GetProductsQuery q, CancellationToken ct)
    {
        var result = await repo.GetPagedAsync(
            q.PageNumber, q.PageSize,
            q.Search, q.CategoryId,
            q.MinPrice, q.MaxPrice,
            q.InStockOnly, q.Status,
            q.SortBy, q.SortDesc, ct);
        return Result.Success(result);
    }
}

// ── Get all categories ────────────────────────────────────────
public sealed record GetCategoriesQuery
    : IRequest<Result<IEnumerable<CategoryDto>>>;

public sealed class GetCategoriesQueryHandler(IProductReadRepository repo)
    : IRequestHandler<GetCategoriesQuery, Result<IEnumerable<CategoryDto>>>
{
    public async Task<Result<IEnumerable<CategoryDto>>> Handle(
        GetCategoriesQuery q, CancellationToken ct)
    {
        var cats = await repo.GetCategoriesAsync(ct);
        return Result.Success(cats);
    }
}
