using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;
using AdminPortal.Infrastructure.MockData;

namespace AdminPortal.Infrastructure.Repositories;

public class MockOrderRepository : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id) =>
        Task.FromResult(MockDataStore.Orders.FirstOrDefault(o => o.Id == id));

    public Task<IEnumerable<Order>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Order>>(MockDataStore.Orders.OrderByDescending(o => o.OrderDate));

    public Task<Order> AddAsync(Order entity)
    {
        MockDataStore.Orders.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<Order> UpdateAsync(Order entity)
    {
        var index = MockDataStore.Orders.FindIndex(o => o.Id == entity.Id);
        if (index >= 0) MockDataStore.Orders[index] = entity;
        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var order = MockDataStore.Orders.FirstOrDefault(o => o.Id == id);
        if (order is null) return Task.FromResult(false);
        MockDataStore.Orders.Remove(order);
        return Task.FromResult(true);
    }

    public Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status) =>
        Task.FromResult<IEnumerable<Order>>(MockDataStore.Orders.Where(o => o.Status == status));

    public Task<IEnumerable<Order>> GetRecentOrdersAsync(int count) =>
        Task.FromResult<IEnumerable<Order>>(MockDataStore.Orders
            .OrderByDescending(o => o.OrderDate).Take(count));

    public Task<decimal> GetTotalRevenueAsync(DateTime from, DateTime to) =>
        Task.FromResult(MockDataStore.Orders
            .Where(o => o.OrderDate >= from && o.OrderDate <= to && o.Status == OrderStatus.Delivered)
            .Sum(o => o.TotalAmount));

    public Task<int> GetOrderCountAsync(DateTime from, DateTime to) =>
        Task.FromResult(MockDataStore.Orders
            .Count(o => o.OrderDate >= from && o.OrderDate <= to));
}
