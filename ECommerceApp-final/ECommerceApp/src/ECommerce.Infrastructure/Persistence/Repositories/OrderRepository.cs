using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class OrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Orders.Include(o => o.Items)
                          .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => await db.Orders.Include(o => o.Items)
                          .Where(o => o.CustomerId == customerId)
                          .OrderByDescending(o => o.CreatedAt)
                          .ToListAsync(ct);

    public async Task AddAsync(Order order, CancellationToken ct = default)
        => await db.Orders.AddAsync(order, ct);

    public void Update(Order order)
    {
        if (db.Entry(order).State == EntityState.Detached)
            db.Entry(order).State = EntityState.Modified;
    }
}
