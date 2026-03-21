using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;

namespace AdminPortal.Application.Interfaces;

public interface IProductService
{
    Task<Result<PagedResult<ProductDto>>> GetProductsAsync(int page, int pageSize, string? search = null, string? category = null);
    Task<Result<ProductDto>> GetProductByIdAsync(Guid id);
    Task<Result<ProductDto>> CreateProductAsync(CreateProductDto dto);
    Task<Result<ProductDto>> UpdateProductAsync(UpdateProductDto dto);
    Task<Result> DeleteProductAsync(Guid id);
    Task<Result<IEnumerable<string>>> GetCategoriesAsync();
}
