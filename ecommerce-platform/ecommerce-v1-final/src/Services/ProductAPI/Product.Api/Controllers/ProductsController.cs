using Common.Domain.Interfaces;
using Common.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Product.Application.Commands;
using Product.Application.DTOs;
using Product.Application.Queries;
using Asp.Versioning;

namespace Product.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/products")]
[ApiVersion("1.0")]
[Produces("application/json")]
public sealed class ProductsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] GetProductsQuery query, CancellationToken ct)
        => Ok((await mediator.Send(query, ct)).Value);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken ct)
    {
        var r = await mediator.Send(new GetProductByIdQuery(id), ct);
        return r.IsSuccess ? Ok(r.Value) : NotFound(r.Error);
    }

    [HttpPost]
    [Authorize(Policy = "VendorOrAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand cmd, CancellationToken ct)
    {
        var r = await mediator.Send(cmd, ct);
        return r.IsSuccess
            ? CreatedAtAction(nameof(GetProduct), new { id = r.Value }, r.Value)
            : Problem(r.Error.Message, statusCode: 400, title: r.Error.Code);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "VendorOrAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand cmd, CancellationToken ct)
    {
        var r = await mediator.Send(cmd with { ProductId = id }, ct);
        return r.IsSuccess ? NoContent() : Problem(r.Error.Message, statusCode: 400, title: r.Error.Code);
    }

    [HttpPatch("{id:guid}/stock")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AdjustStock(
        Guid id, [FromBody] StockRequest req, CancellationToken ct)
    {
        var r = await mediator.Send(new AdjustStockCommand(id, req.Delta, req.Reason), ct);
        return r.IsSuccess ? NoContent() : Problem(r.Error.Message, statusCode: 422, title: r.Error.Code);
    }
}

[ApiController]
[Route("api/v{version:apiVersion}/categories")]
[ApiVersion("1.0")]
public sealed class CategoriesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok((await mediator.Send(new GetCategoriesQuery(), ct)).Value);

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryCommand cmd, CancellationToken ct)
    {
        var r = await mediator.Send(cmd, ct);
        return r.IsSuccess ? StatusCode(201, r.Value) : Problem(r.Error.Message);
    }
}

public sealed record StockRequest(int Delta, string Reason);
