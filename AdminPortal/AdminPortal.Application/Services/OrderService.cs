using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;

namespace AdminPortal.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;

    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<PagedResult<OrderDto>>> GetOrdersAsync(int page, int pageSize, OrderStatus? status = null)
    {
        var orders = status.HasValue
            ? await _orderRepository.GetByStatusAsync(status.Value)
            : await _orderRepository.GetAllAsync();

        var total = orders.Count();
        var paged = orders.Skip((page - 1) * pageSize).Take(pageSize).Select(MapToDto).ToList();

        return Result<PagedResult<OrderDto>>.Success(new PagedResult<OrderDto>
        {
            Items = paged,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<OrderDto>> GetOrderByIdAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        return order is null
            ? Result<OrderDto>.Failure("Order not found.")
            : Result<OrderDto>.Success(MapToDto(order));
    }

    public async Task<Result<OrderDto>> UpdateOrderStatusAsync(Guid id, OrderStatus status)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order is null)
            return Result<OrderDto>.Failure("Order not found.");

        order.Status = status;
        var updated = await _orderRepository.UpdateAsync(order);
        return Result<OrderDto>.Success(MapToDto(updated));
    }

    public async Task<Result<IEnumerable<OrderDto>>> GetRecentOrdersAsync(int count = 10)
    {
        var orders = await _orderRepository.GetRecentOrdersAsync(count);
        return Result<IEnumerable<OrderDto>>.Success(orders.Select(MapToDto));
    }

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        CustomerName = o.CustomerName,
        CustomerEmail = o.CustomerEmail,
        TotalAmount = o.TotalAmount,
        Status = o.Status.ToString(),
        StatusEnum = o.Status,
        PaymentMethod = o.PaymentMethod,
        OrderDate = o.OrderDate,
        Items = o.Items.Select(i => new OrderItemDto
        {
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };
}
