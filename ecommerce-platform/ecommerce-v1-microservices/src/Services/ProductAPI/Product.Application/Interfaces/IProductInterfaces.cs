using Product.Application.DTOs;
using Product.Domain.Entities;

// Alias resolves the namespace/class name collision:
// "Product" is both a root namespace AND a class in Product.Domain.Entities
using ProductEntity   = Product.Domain.Entities.Product;
using CategoryEntity  = Product.Domain.Entities.Category;

namespace Product.Application.Interfaces;

public interface IProductRepository
{
    Task<ProductEntity?>          GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool>                    SkuExistsAsync(string sku, CancellationToken ct = default);
    void Add(ProductEntity product);
    void Update(ProductEntity product);
    void Delete(ProductEntity product);
}

public interface ICategoryRepository
{
    Task<CategoryEntity?>           GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool>                      SlugExistsAsync(string slug, CancellationToken ct = default);
    Task<IEnumerable<CategoryEntity>> GetAllAsync(CancellationToken ct = default);
    void Add(CategoryEntity category);
}

public interface IProductReadRepository
{
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ProductSummaryDto>> GetPagedAsync(
        int page, int size,
        string? search, Guid? categoryId,
        decimal? minPrice, decimal? maxPrice,
        bool inStockOnly, string? status,
        string? sortBy, bool sortDesc,
        CancellationToken ct = default);
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
}

public interface IUnitOfWorkProduct
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
