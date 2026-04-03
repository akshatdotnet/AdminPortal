using System.Linq.Expressions;
using Zovo.Core.Entities;

namespace Zovo.Core.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    IQueryable<T> Query();
}

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySlugAsync(string slug);
    Task<IEnumerable<string>> GetCategoriesAsync();
    Task<IEnumerable<Product>> GetLowStockAsync(int threshold = 10);
}

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetWithDetailsAsync(int id);
    Task<IEnumerable<Order>> GetByCustomerAsync(int customerId);
    Task<string> GenerateOrderNumberAsync();
    Task<decimal> GetTotalRevenueAsync(DateTime? from = null, DateTime? to = null);
}

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetWithOrdersAsync(int id);
    Task<Customer?> GetByEmailAsync(string email);
}

public interface IUnitOfWork : IDisposable
{
    IProductRepository  Products      { get; }
    IOrderRepository    Orders        { get; }
    ICustomerRepository Customers     { get; }
    IRepository<StoreSettings> StoreSettings { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
