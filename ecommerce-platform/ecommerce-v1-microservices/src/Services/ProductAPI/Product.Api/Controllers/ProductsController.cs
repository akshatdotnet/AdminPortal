using Asp.Versioning;
using Common.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Product.Application.Commands;
using Product.Application.DTOs;
using Product.Application.Queries;
using ProductEntity   = Product.Domain.Entities.Product;
using CategoryEntity  = Product.Domain.Entities.Category;
using ProductImageEntity = Product.Domain.Entities.ProductImage;

namespace Product.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/products")]
[ApiVersion("1.0")]
[Produces("application/json")]
public sealed class ProductsController(IMediator mediator) : ControllerBase
{
    /// <summary>Get paginated product list with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductSummaryDto>), 200)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] GetProductsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return Ok(result.Value);
    }

    /// <summary>Get a single product by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductByIdQuery(id), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Message, statusCode: 404, title: result.Error.Code);
    }

    /// <summary>Create a new product. Requires Vendor or Admin role.</summary>
    [HttpPost]
    [Authorize(Policy = "VendorOrAdmin")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetProduct), new { id = result.Value }, result.Value)
            : Problem(result.Error.Message,
                statusCode: result.Error.Code.Contains("Conflict") ? 409 : 400,
                title: result.Error.Code);
    }

    /// <summary>Update product details. Requires Vendor or Admin role.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "VendorOrAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateProductCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd with { ProductId = id }, ct);
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message,
                statusCode: result.Error.Code.Contains("NotFound") ? 404 : 400,
                title: result.Error.Code);
    }

    /// <summary>Soft-delete a product. Requires Admin role.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteProductCommand(id), ct);
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: 404, title: result.Error.Code);
    }

    /// <summary>Adjust stock level (positive = add, negative = reduce).</summary>
    [HttpPatch("{id:guid}/stock")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(204)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> AdjustStock(
        Guid id, [FromBody] StockAdjustRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new AdjustStockCommand(id, req.Delta, req.Reason), ct);
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message,
                statusCode: result.Error.Code.Contains("NotFound") ? 404 : 422,
                title: result.Error.Code);
    }

    /// <summary>Activate or deactivate a product.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "VendorOrAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ChangeStatus(
        Guid id, [FromBody] StatusChangeRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new ChangeProductStatusCommand(id, req.Action), ct);
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message,
                statusCode: result.Error.Code.Contains("NotFound") ? 404 : 400,
                title: result.Error.Code);
    }
}

[ApiController]
[Route("api/v{version:apiVersion}/categories")]
[ApiVersion("1.0")]
[Produces("application/json")]
public sealed class CategoriesController(IMediator mediator) : ControllerBase
{
    /// <summary>Get all categories with product counts.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), 200)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCategoriesQuery(), ct);
        return Ok(result.Value);
    }

    /// <summary>Create a new category. Requires Admin role.</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd, ct);
        return result.IsSuccess
            ? StatusCode(201, result.Value)
            : Problem(result.Error.Message,
                statusCode: result.Error.Code.Contains("Conflict") ? 409 : 400,
                title: result.Error.Code);
    }
}

public sealed record StockAdjustRequest(int Delta, string Reason);
public sealed record StatusChangeRequest(string Action);
