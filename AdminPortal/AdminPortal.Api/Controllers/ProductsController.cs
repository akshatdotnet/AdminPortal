using AdminPortal.Api.DTOs;
using AdminPortal.Api.MockData;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly MockDataStore _store;

    public ProductsController(MockDataStore store) => _store = store;

    /// <summary>Get paginated/filtered product list.</summary>
    [HttpGet]
    public ActionResult<PagedResponse<ProductDto>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _store.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                                  || p.Category.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var total = query.Count();
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Map)
            .ToList();

        return Ok(new PagedResponse<ProductDto>
        {
            Data = items, TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    /// <summary>Get a single product by ID.</summary>
    [HttpGet("{id:int}")]
    public ActionResult<ApiResponse<ProductDto>> GetById(int id)
    {
        var product = _store.Products.FirstOrDefault(p => p.Id == id);
        if (product is null)
            return NotFound(new ApiResponse<ProductDto> { Success = false, Message = "Product not found." });

        return Ok(new ApiResponse<ProductDto> { Data = Map(product) });
    }

    /// <summary>Get all distinct categories.</summary>
    [HttpGet("categories")]
    public ActionResult<ApiResponse<IEnumerable<string>>> GetCategories()
    {
        var cats = _store.Products.Select(p => p.Category).Distinct().OrderBy(c => c);
        return Ok(new ApiResponse<IEnumerable<string>> { Data = cats });
    }

    /// <summary>Toggle active/inactive status.</summary>
    [HttpPatch("{id:int}/toggle")]
    public ActionResult<ApiResponse<ProductDto>> Toggle(int id)
    {
        var product = _store.Products.FirstOrDefault(p => p.Id == id);
        if (product is null)
            return NotFound(new ApiResponse<ProductDto> { Success = false, Message = "Product not found." });

        product.IsActive = !product.IsActive;
        return Ok(new ApiResponse<ProductDto> { Data = Map(product), Message = $"Product is now {(product.IsActive ? "active" : "inactive")}." });
    }

    private static ProductDto Map(Product p) => new()
    {
        Id = p.Id, Name = p.Name, Description = p.Description,
        Price = p.Price, Stock = p.Stock, Category = p.Category,
        ImageUrl = p.ImageUrl, IsActive = p.IsActive, CreatedAt = p.CreatedAt
    };
}
