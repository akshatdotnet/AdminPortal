using AdminPortal.Domain.Entities;

namespace AdminPortal.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
    Task<IEnumerable<Order>> GetRecentOrdersAsync(int count);
    Task<decimal> GetTotalRevenueAsync(DateTime from, DateTime to);
    Task<int> GetOrderCountAsync(DateTime from, DateTime to);
}
