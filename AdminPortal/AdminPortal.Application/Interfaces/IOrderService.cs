using AdminPortal.Application.Common;
using AdminPortal.Application.DTOs;
using AdminPortal.Domain.Entities;

namespace AdminPortal.Application.Interfaces;

public interface IOrderService
{
    Task<Result<PagedResult<OrderDto>>> GetOrdersAsync(int page, int pageSize, OrderStatus? status = null);
    Task<Result<OrderDto>> GetOrderByIdAsync(Guid id);
    Task<Result<OrderDto>> UpdateOrderStatusAsync(Guid id, OrderStatus status);
    Task<Result<IEnumerable<OrderDto>>> GetRecentOrdersAsync(int count = 10);
}
