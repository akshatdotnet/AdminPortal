using Common.Domain.Interfaces;
using Common.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Commands;
using Order.Application.DTOs;
using Order.Infrastructure.Persistence;
using System.Security.Claims;
using Asp.Versioning;

namespace Order.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/orders")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
public sealed class OrdersController(IMediator mediator, OrderQueryService queryService)
    : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderCommand cmd, CancellationToken ct)
    {
        var r = await mediator.Send(cmd with { CustomerId = UserId }, ct);
        return r.IsSuccess
            ? CreatedAtAction(nameof(GetOrder), new { id = r.Value.OrderId }, r.Value)
            : Problem(r.Error.Message, statusCode: 422, title: r.Error.Code);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var dto = await queryService.GetOrderDtoAsync(id, ct);
        if (dto is null) return NotFound();
        if (!IsAdmin && dto.CustomerId != UserId) return Forbid();
        return Ok(dto);
    }

    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await queryService.GetCustomerOrdersAsync(
            UserId, pageNumber, pageSize, status, ct);
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await queryService.GetAllOrdersAsync(pageNumber, pageSize, status, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelRequest req, CancellationToken ct)
    {
        var r = await mediator.Send(new CancelOrderCommand(id, UserId, req.Reason), ct);
        return r.IsSuccess ? NoContent() : Problem(r.Error.Message, statusCode: 422, title: r.Error.Code);
    }

    [HttpPost("{id:guid}/ship")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Ship(
        Guid id, [FromBody] ShipRequest req, CancellationToken ct)
    {
        var r = await mediator.Send(new ShipOrderCommand(id, req.TrackingNumber, req.Carrier), ct);
        return r.IsSuccess ? NoContent() : Problem(r.Error.Message, statusCode: 422, title: r.Error.Code);
    }

    [HttpPost("{id:guid}/deliver")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Deliver(Guid id, CancellationToken ct)
    {
        var r = await mediator.Send(new DeliverOrderCommand(id), ct);
        return r.IsSuccess ? NoContent() : Problem(r.Error.Message, statusCode: 422, title: r.Error.Code);
    }

    [HttpPost("{id:guid}/confirm-payment")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmPayment(
        Guid id, [FromBody] ConfirmPaymentRequest req, CancellationToken ct)
    {
        var r = await mediator.Send(new ConfirmPaymentCommand(id, req.PaymentIntentId), ct);
        return r.IsSuccess ? Ok() : Problem(r.Error.Message, statusCode: 422, title: r.Error.Code);
    }
}

public sealed record CancelRequest(string Reason);
public sealed record ShipRequest(string TrackingNumber, string Carrier);
public sealed record ConfirmPaymentRequest(string PaymentIntentId);
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
