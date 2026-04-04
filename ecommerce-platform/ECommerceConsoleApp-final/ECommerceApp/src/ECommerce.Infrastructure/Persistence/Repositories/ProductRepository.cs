using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence.Repositories;

public class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
        => await db.Products.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct = default)
        => await db.Products.AsNoTracking().Where(p => p.CategoryId == categoryId).ToListAsync(ct);

    /// <summary>Tracked load — use when you need to mutate the product (Reserve, Release).</summary>
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Products.FindAsync([id], ct);

    /// <summary>AsNoTracking load — use for read-only validation. Avoids tracking OwnsOne Price.</summary>
    public async Task<Product?> GetByIdReadOnlyAsync(Guid id, CancellationToken ct = default)
        => await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Product>> SearchAsync(string term, CancellationToken ct = default)
        => await db.Products.AsNoTracking()
            .Where(p => p.Name.Contains(term) || p.Description.Contains(term))
            .ToListAsync(ct);

    public async Task AddAsync(Product product, CancellationToken ct = default)
        => await db.Products.AddAsync(product, ct);

    public void Update(Product product)
    {
        if (db.Entry(product).State == EntityState.Detached)
            db.Entry(product).State = EntityState.Modified;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await db.Products.AnyAsync(p => p.Id == id, ct);
}
