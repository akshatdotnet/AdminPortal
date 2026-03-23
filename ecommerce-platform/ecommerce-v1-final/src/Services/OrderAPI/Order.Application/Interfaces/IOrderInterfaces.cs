using Common.Domain.Primitives;
using Order.Application.DTOs;

using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Application.Interfaces;

public interface IOrderRepository
{
    Task<OrderEntity?>              GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<OrderEntity>>  GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    void Add(OrderEntity order);
    void Update(OrderEntity order);
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
    Task<Result<decimal>> ValidateAsync(
        string code, decimal amount, CancellationToken ct = default);
}
