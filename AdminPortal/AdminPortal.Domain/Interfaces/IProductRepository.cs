using AdminPortal.Domain.Entities;

namespace AdminPortal.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetActiveProductsAsync();
    Task<IEnumerable<Product>> GetBycategoryAsync(string category);
    Task<IEnumerable<Product>> SearchAsync(string query);
    Task<int> GetTotalCountAsync();
}
