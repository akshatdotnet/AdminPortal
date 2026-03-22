using Cart.Domain.Entities;
using Common.Domain.Primitives;

namespace Cart.Application.Interfaces;

public interface ICartRepository
{
    Task<ShoppingCart?> GetAsync(Guid customerId, CancellationToken ct = default);
    Task<ShoppingCart> GetOrCreateAsync(Guid customerId, CancellationToken ct = default);
    Task SaveAsync(ShoppingCart cart, CancellationToken ct = default);
    Task DeleteAsync(Guid customerId, CancellationToken ct = default);
}

public interface ICouponServiceClient
{
    Task<Result<decimal>> ValidateAsync(string code, decimal amount, CancellationToken ct = default);
}
