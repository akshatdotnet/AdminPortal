using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STHEnterprise.Application.Interfaces;
using System.Security.Claims;

namespace STHEnterprise.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    private string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // 🔹 My Orders
    [HttpGet]
    public IActionResult MyOrders()
    {
        var result = _orders.GetMyOrders(UserId);
        return Ok(result);
    }

    // 🔹 Order Details
    [HttpGet("{orderId}")]
    public IActionResult OrderDetails(Guid orderId)
    {
        var order = _orders.GetOrderById(UserId, orderId);
        if (order == null) return NotFound("Order not found");

        return Ok(order);
    }
}
