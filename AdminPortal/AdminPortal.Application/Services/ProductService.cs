using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;

namespace AdminPortal.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<PagedResult<ProductDto>>> GetProductsAsync(int page, int pageSize, string? search = null, string? category = null)
    {
        var products = string.IsNullOrWhiteSpace(search)
            ? await _productRepository.GetAllAsync()
            : await _productRepository.SearchAsync(search);

        if (!string.IsNullOrWhiteSpace(category))
            products = products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        var total = products.Count();
        var paged = products.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(MapToDto).ToList();

        return Result<PagedResult<ProductDto>>.Success(new PagedResult<ProductDto>
        {
            Items = paged,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<ProductDto>> GetProductByIdAsync(Guid id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        return product is null
            ? Result<ProductDto>.Failure("Product not found.")
            : Result<ProductDto>.Success(MapToDto(product));
    }

    public async Task<Result<ProductDto>> CreateProductAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            DiscountedPrice = dto.DiscountedPrice,
            Stock = dto.Stock,
            Category = dto.Category,
            ImageUrl = dto.ImageUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var created = await _productRepository.AddAsync(product);
        return Result<ProductDto>.Success(MapToDto(created));
    }

    public async Task<Result<ProductDto>> UpdateProductAsync(UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(dto.Id);
        if (product is null)
            return Result<ProductDto>.Failure("Product not found.");

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.DiscountedPrice = dto.DiscountedPrice;
        product.Stock = dto.Stock;
        product.Category = dto.Category;
        product.ImageUrl = dto.ImageUrl;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        var updated = await _productRepository.UpdateAsync(product);
        return Result<ProductDto>.Success(MapToDto(updated));
    }

    public async Task<Result> DeleteProductAsync(Guid id)
    {
        var deleted = await _productRepository.DeleteAsync(id);
        return deleted ? Result.Success() : Result.Failure("Product not found.");
    }

    public async Task<Result<IEnumerable<string>>> GetCategoriesAsync()
    {
        var products = await _productRepository.GetAllAsync();
        var categories = products.Select(p => p.Category).Distinct().OrderBy(c => c);
        return Result<IEnumerable<string>>.Success(categories);
    }

    private static ProductDto MapToDto(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Price = p.Price,
        DiscountedPrice = p.DiscountedPrice,
        Stock = p.Stock,
        Category = p.Category,
        ImageUrl = p.ImageUrl,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt
    };
}
