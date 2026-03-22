using Common.Domain.Interfaces;
using Product.Application.DTOs;
using Product.Domain.Entities;

namespace Product.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct = default);
    void Add(Product product);
    void Update(Product product);
}

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Category>> GetAllAsync(CancellationToken ct = default);
    void Add(Category category);
}

public interface IProductReadRepository
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ProductSummaryDto>> GetPagedAsync(
        int page, int size, string? search, Guid? categoryId,
        decimal? minPrice, decimal? maxPrice, bool inStockOnly,
        CancellationToken ct = default);
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
}

public interface IUnitOfWorkProduct
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
