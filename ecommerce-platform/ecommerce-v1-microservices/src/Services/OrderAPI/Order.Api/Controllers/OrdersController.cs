using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Commands;
using Order.Application.DTOs;
using Order.Infrastructure.Persistence;
using System.Security.Claims;

namespace Order.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/orders")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
public sealed class OrdersController(
    IMediator mediator,
    OrderQueryService queryService) : ControllerBase
{
    private Guid UserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpPost]
    [ProducesResponseType(typeof(PlaceOrderResponse), 201)]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderCommand cmd, CancellationToken ct)
    {
        var result = await mediator.Send(cmd with { CustomerId = UserId }, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetOrder),
                new { id = result.Value.OrderId }, result.Value)
            : Problem(result.Error.Message, statusCode: 422, title: result.Error.Code);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), 200)]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var dto = await queryService.GetOrderDtoAsync(id, ct);
        if (dto is null) return NotFound();
        if (!IsAdmin && dto.CustomerId != UserId) return Forbid();
        return Ok(dto);
    }

    [HttpGet("my-orders")]
    [ProducesResponseType(typeof(PagedResult<OrderSummaryDto>), 200)]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize   = 10,
        [FromQuery] string? status = null,
        CancellationToken ct       = default)
    {
        var result = await queryService.GetCustomerOrdersAsync(
            UserId, pageNumber, pageSize, status, ct);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PagedResult<OrderSummaryDto>), 200)]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize   = 20,
        [FromQuery] string? status = null,
        CancellationToken ct       = default)
    {
        var result = await queryService.GetAllOrdersAsync(
            pageNumber, pageSize, status, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new CancelOrderCommand(id, UserId, req.Reason), ct);
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: 422, title: result.Error.Code);
    }

    [HttpPost("{id:guid}/ship")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Ship(
        Guid id, [FromBody] ShipRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new ShipOrderCommand(id, req.TrackingNumber, req.Carrier), ct);
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: 422, title: result.Error.Code);
    }

    [HttpPost("{id:guid}/deliver")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Deliver(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeliverOrderCommand(id), ct);
        return result.IsSuccess
            ? NoContent()
            : Problem(result.Error.Message, statusCode: 422, title: result.Error.Code);
    }

    [HttpPost("{id:guid}/confirm-payment")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmPayment(
        Guid id, [FromBody] ConfirmPaymentRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(
            new ConfirmPaymentCommand(id, req.PaymentIntentId), ct);
        return result.IsSuccess
            ? Ok()
            : Problem(result.Error.Message, statusCode: 422, title: result.Error.Code);
    }
}

public sealed record CancelRequest(string Reason);
public sealed record ShipRequest(string TrackingNumber, string Carrier);
public sealed record ConfirmPaymentRequest(string PaymentIntentId);
