using STHEnterprise.Application.DTOs.Orders;

namespace STHEnterprise.Application.Interfaces;

public interface IOrderService
{
    List<OrderListDto> GetMyOrders(string userId);
    OrderDetailDto? GetOrderById(string userId, Guid orderId);
}
