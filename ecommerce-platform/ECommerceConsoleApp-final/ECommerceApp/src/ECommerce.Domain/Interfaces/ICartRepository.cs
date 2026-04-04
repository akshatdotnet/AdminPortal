using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

public interface ICartRepository
{
    /// <summary>Load cart WITH items (tracked) — use when iterating or clearing items.</summary>
    Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>Load cart WITHOUT items — use for simple add operations to avoid tracking CartItems.</summary>
    Task<Cart?> GetByCustomerIdNoItemsAsync(Guid customerId, CancellationToken ct = default);

    Task<Cart?> GetByIdAsync(Guid cartId, CancellationToken ct = default);
    Task AddAsync(Cart cart, CancellationToken ct = default);
    void Update(Cart cart);
    void Remove(Cart cart);
}
