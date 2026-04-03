using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

public interface ICartItemRepository
{
    Task<List<CartItem>> GetByCartIdAsync(Guid cartId, CancellationToken ct = default);
    Task<CartItem?> FindByCartAndProductAsync(Guid cartId, Guid productId, CancellationToken ct = default);
    Task AddAsync(CartItem item, CancellationToken ct = default);
    void Remove(CartItem item);
}
