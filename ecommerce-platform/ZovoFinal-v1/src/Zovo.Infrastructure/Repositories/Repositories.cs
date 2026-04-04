using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Zovo.Core.Entities;
using Zovo.Core.Interfaces;
using Zovo.Infrastructure.Data;

namespace Zovo.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ZovoDbContext _db;
    protected readonly DbSet<T> _set;

    public Repository(ZovoDbContext db) { _db = db; _set = db.Set<T>(); }

    public async Task<T?> GetByIdAsync(int id)          => await _set.FindAsync(id);
    public async Task<IEnumerable<T>> GetAllAsync()      => await _set.AsNoTracking().ToListAsync();
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> p)
        => await _set.AsNoTracking().Where(p).ToListAsync();
    public async Task<T> AddAsync(T entity)              { _set.Add(entity); return entity; }
    public Task UpdateAsync(T entity)                    { _set.Update(entity); return Task.CompletedTask; }
    public Task DeleteAsync(T entity)                    { _set.Remove(entity); return Task.CompletedTask; }
    public async Task<int> CountAsync(Expression<Func<T, bool>>? p = null)
        => p is null ? await _set.CountAsync() : await _set.CountAsync(p);
    public IQueryable<T> Query()                         => _set.AsQueryable();
}

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ZovoDbContext db) : base(db) { }

    public async Task<Product?> GetBySlugAsync(string slug)
        => await _set.FirstOrDefaultAsync(p => p.Slug == slug);

    public async Task<IEnumerable<string>> GetCategoriesAsync()
        => await _set.Select(p => p.Category).Distinct().OrderBy(c => c).ToListAsync();

    public async Task<IEnumerable<Product>> GetLowStockAsync(int threshold = 10)
        => await _set.AsNoTracking().Where(p => p.IsActive && p.Stock <= threshold).OrderBy(p => p.Stock).ToListAsync();
}

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ZovoDbContext db) : base(db) { }

    public async Task<Order?> GetWithDetailsAsync(int id)
        => await _set.Include(o => o.Customer)
                     .Include(o => o.Items).ThenInclude(i => i.Product)
                     .Include(o => o.ShippingAddress)
                     .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IEnumerable<Order>> GetByCustomerAsync(int customerId)
        => await _set.AsNoTracking().Where(o => o.CustomerId == customerId).OrderByDescending(o => o.CreatedAt).ToListAsync();

    public async Task<string> GenerateOrderNumberAsync()
    {
        var count = await _set.CountAsync() + 1;
        return $"ZOV-{count:D6}";
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime? from = null, DateTime? to = null)
    {
        var q = _set.Where(o => o.PaymentStatus == Core.Enums.PaymentStatus.Paid);
        if (from.HasValue) q = q.Where(o => o.CreatedAt >= from);
        if (to.HasValue)   q = q.Where(o => o.CreatedAt <= to);
        return await q.SumAsync(o => o.TotalAmount);
    }
}

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ZovoDbContext db) : base(db) { }

    public async Task<Customer?> GetWithOrdersAsync(int id)
        => await _set.Include(c => c.Orders).Include(c => c.Addresses).FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Customer?> GetByEmailAsync(string email)
        => await _set.FirstOrDefaultAsync(c => c.Email == email);
}
