using Common.Domain.Primitives;
using Order.Application.DTOs;
using Order.Domain.Entities;

namespace Order.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    void Add(Order order);
    void Update(Order order);
}

public interface IUnitOfWorkOrder
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IProductServiceClient
{
    Task<Result> ReserveStockAsync(Guid productId, int quantity, CancellationToken ct = default);
    Task<Result> ReleaseStockAsync(Guid productId, int quantity, CancellationToken ct = default);
}

public interface ICouponServiceClient
{
    Task<Result<decimal>> ValidateAsync(string code, decimal amount, CancellationToken ct = default);
}
