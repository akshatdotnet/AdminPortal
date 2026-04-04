using System.Linq.Expressions;
using Zovo.Core.Entities;
using Zovo.Core.Enums;
using Zovo.Core.ValueObjects;

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

    //Task<ServiceResult> CreateAsync(CreateOrderDto dto);

    //Task<PagedResult<OrderListItemDto>> GetPagedAsync(OrderQueryParams q);
    //Task<OrderDetailDto?> GetDetailAsync(int id);
    //Task<OrderDetailDto?> GetEditAsync(int id);    
    //Task<ServiceResult> UpdateAsync(int id, EditOrderDto dto);
    //Task<ServiceResult> UpdateStatusAsync(int id, OrderStatus status);
    //Task<ServiceResult> CancelAsync(int id);
    //Task<ServiceResult> DeleteAsync(int id);

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
