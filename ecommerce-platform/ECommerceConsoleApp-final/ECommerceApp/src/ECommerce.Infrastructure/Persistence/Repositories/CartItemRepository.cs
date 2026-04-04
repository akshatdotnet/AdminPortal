using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class CartItemRepository(AppDbContext db) : ICartItemRepository
{
    public async Task<List<CartItem>> GetByCartIdAsync(Guid cartId, CancellationToken ct = default)
        => await db.CartItems.Where(i => i.CartId == cartId).ToListAsync(ct);

    public async Task<CartItem?> FindByCartAndProductAsync(
        Guid cartId, Guid productId, CancellationToken ct = default)
        => await db.CartItems
                   .FirstOrDefaultAsync(i => i.CartId == cartId && i.ProductId == productId, ct);

    public async Task AddAsync(CartItem item, CancellationToken ct = default)
        => await db.CartItems.AddAsync(item, ct);

    public void Remove(CartItem item)
        => db.CartItems.Remove(item);
}
