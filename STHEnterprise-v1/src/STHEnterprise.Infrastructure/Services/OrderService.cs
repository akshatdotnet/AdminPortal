using STHEnterprise.Application.DTOs.Orders;
using STHEnterprise.Application.Interfaces;
using STHEnterprise.Core.Entities;

namespace STHEnterprise.Infrastructure.Services;

public class OrderService : IOrderService
{
    private static readonly List<Order> Orders = new()
    {
        new Order
        {
            UserId = "user-1",
            TotalAmount = 1299,
            Status = "Delivered",
            Items = new()
            {
                new OrderItem { ProductId = 1, ProductName = "Tomato", Quantity = 2, Price = 50 },
                new OrderItem { ProductId = 2, ProductName = "Potato", Quantity = 3, Price = 40 }
            }
        },
        new Order
        {
            UserId = "user-1",
            TotalAmount = 799,
            Status = "Confirmed",
            Items = new()
            {
                new OrderItem { ProductId = 3, ProductName = "Apple", Quantity = 1, Price = 120 }
            }
        }
    };

    public List<OrderListDto> GetMyOrders(string userId)
    {
        return Orders
            .Where(o => o.UserId == userId)
            .Select(o => new OrderListDto
            {
                OrderId = o.Id,
                Date = o.CreatedAt,
                Amount = o.TotalAmount,
                Status = o.Status
            }).ToList();
    }

    public OrderDetailDto? GetOrderById(string userId, Guid orderId)
    {
        var order = Orders.FirstOrDefault(o => o.UserId == userId && o.Id == orderId);
        if (order == null) return null;

        return new OrderDetailDto
        {
            OrderId = order.Id,
            Date = order.CreatedAt,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Name = i.ProductName,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        };
    }
}
