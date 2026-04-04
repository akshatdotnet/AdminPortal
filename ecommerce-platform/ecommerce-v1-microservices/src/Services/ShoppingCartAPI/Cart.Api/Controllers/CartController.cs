using Cart.Application.Commands;
using Cart.Application.DTOs;
using Common.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Asp.Versioning;

namespace Cart.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/cart")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
public sealed class CartController(IMediator mediator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [ProducesResponseType(typeof(CartDto), 200)]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var r = await mediator.Send(new GetCartQuery(UserId), ct);
        return r.IsSuccess ? Ok(r.Value) : NotFound();
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem(
        [FromBody] AddToCartCommand cmd, CancellationToken ct)
    {
        var r = await mediator.Send(cmd with { CustomerId = UserId }, ct);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
    }

    [HttpPut("items/{productId:guid}")]
    public async Task<IActionResult> UpdateItem(
        Guid productId, [FromBody] UpdateQtyRequest req, CancellationToken ct)
    {
        var r = await mediator.Send(
            new UpdateCartItemCommand(UserId, productId, req.Quantity), ct);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken ct)
    {
        var r = await mediator.Send(new RemoveCartItemCommand(UserId, productId), ct);
        return r.IsSuccess ? Ok(r.Value) : NotFound();
    }

    [HttpPost("coupon")]
    public async Task<IActionResult> ApplyCoupon(
        [FromBody] CouponRequest req, CancellationToken ct)
    {
        var r = await mediator.Send(new ApplyCouponCommand(UserId, req.CouponCode), ct);
        return r.IsSuccess ? Ok(r.Value) : BadRequest(r.Error);
    }

    [HttpDelete("coupon")]
    public async Task<IActionResult> RemoveCoupon(CancellationToken ct)
    {
        var r = await mediator.Send(new RemoveCouponCommand(UserId), ct);
        return r.IsSuccess ? Ok(r.Value) : NotFound();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        await mediator.Send(new ClearCartCommand(UserId), ct);
        return NoContent();
    }
}

public sealed record UpdateQtyRequest(int Quantity);
public sealed record CouponRequest(string CouponCode);
