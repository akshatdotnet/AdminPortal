using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class CartRepository(AppDbContext db) : ICartRepository
{
    /// <summary>Load cart WITH items populated. Used for ViewCart, PlaceOrder, RemoveItem.</summary>
    public async Task<Cart?> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
        if (cart is null) return null;
        var items = await db.CartItems.Where(i => i.CartId == cart.Id).ToListAsync(ct);
        cart.LoadItems(items);
        return cart;
    }

    /// <summary>Load cart shell WITHOUT items — used by AddToCartCommandHandler.</summary>
    public async Task<Cart?> GetByCustomerIdNoItemsAsync(Guid customerId, CancellationToken ct = default)
        => await db.Carts.FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

    /// <summary>Load cart by ID with items populated.</summary>
    public async Task<Cart?> GetByIdAsync(Guid cartId, CancellationToken ct = default)
    {
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.Id == cartId, ct);
        if (cart is null) return null;
        var items = await db.CartItems.Where(i => i.CartId == cart.Id).ToListAsync(ct);
        cart.LoadItems(items);
        return cart;
    }

    public async Task AddAsync(Cart cart, CancellationToken ct = default)
        => await db.Carts.AddAsync(cart, ct);

    public void Update(Cart cart)
    {
        if (db.Entry(cart).State == EntityState.Detached)
            db.Entry(cart).State = EntityState.Modified;
    }

    public void Remove(Cart cart)
        => db.Carts.Remove(cart);
}
