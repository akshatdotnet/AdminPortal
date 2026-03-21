using AdminPortal.Api.DTOs;
using AdminPortal.Api.MockData;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly MockDataStore _store;

    public OrdersController(MockDataStore store) => _store = store;

    /// <summary>Get paginated/filtered orders.</summary>
    [HttpGet]
    public ActionResult<PagedResponse<OrderDto>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _store.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed))
            query = query.Where(o => o.Status == parsed);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                                  || o.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase));

        query = query.OrderByDescending(o => o.OrderDate);

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).Select(Map).ToList();

        return Ok(new PagedResponse<OrderDto>
        {
            Data = items, TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    /// <summary>Get a single order by ID.</summary>
    [HttpGet("{id:int}")]
    public ActionResult<ApiResponse<OrderDto>> GetById(int id)
    {
        var order = _store.Orders.FirstOrDefault(o => o.Id == id);
        if (order is null)
            return NotFound(new ApiResponse<OrderDto> { Success = false, Message = "Order not found." });

        return Ok(new ApiResponse<OrderDto> { Data = Map(order) });
    }

    /// <summary>Update order status.</summary>
    [HttpPatch("{id:int}/status")]
    public ActionResult<ApiResponse<OrderDto>> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var order = _store.Orders.FirstOrDefault(o => o.Id == id);
        if (order is null)
            return NotFound(new ApiResponse<OrderDto> { Success = false, Message = "Order not found." });

        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
            return BadRequest(new ApiResponse<OrderDto> { Success = false, Message = "Invalid status value." });

        order.Status = newStatus;
        return Ok(new ApiResponse<OrderDto> { Data = Map(order), Message = "Status updated." });
    }

    private static OrderDto Map(Order o) => new()
    {
        Id = o.Id, OrderNumber = o.OrderNumber, CustomerName = o.CustomerName,
        CustomerPhone = o.CustomerPhone, TotalAmount = o.TotalAmount,
        Status = o.Status.ToString(), OrderDate = o.OrderDate,
        Items = o.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId, ProductName = i.ProductName,
            Quantity = i.Quantity, UnitPrice = i.UnitPrice
        }).ToList()
    };
}

public record UpdateStatusRequest(string Status);
