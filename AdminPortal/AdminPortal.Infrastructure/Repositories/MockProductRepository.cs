using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;
using AdminPortal.Infrastructure.MockData;

namespace AdminPortal.Infrastructure.Repositories;

public class MockProductRepository : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id) =>
        Task.FromResult(MockDataStore.Products.FirstOrDefault(p => p.Id == id));

    public Task<IEnumerable<Product>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Product>>(MockDataStore.Products.OrderByDescending(p => p.CreatedAt));

    public Task<Product> AddAsync(Product entity)
    {
        MockDataStore.Products.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<Product> UpdateAsync(Product entity)
    {
        var index = MockDataStore.Products.FindIndex(p => p.Id == entity.Id);
        if (index >= 0) MockDataStore.Products[index] = entity;
        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var product = MockDataStore.Products.FirstOrDefault(p => p.Id == id);
        if (product is null) return Task.FromResult(false);
        MockDataStore.Products.Remove(product);
        return Task.FromResult(true);
    }

    public Task<IEnumerable<Product>> GetActiveProductsAsync() =>
        Task.FromResult<IEnumerable<Product>>(MockDataStore.Products.Where(p => p.IsActive));

    public Task<IEnumerable<Product>> GetBycategoryAsync(string category) =>
        Task.FromResult<IEnumerable<Product>>(MockDataStore.Products.Where(p =>
            p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)));

    public Task<IEnumerable<Product>> SearchAsync(string query) =>
        Task.FromResult<IEnumerable<Product>>(MockDataStore.Products.Where(p =>
            p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Category.Contains(query, StringComparison.OrdinalIgnoreCase)));

    public Task<int> GetTotalCountAsync() =>
        Task.FromResult(MockDataStore.Products.Count);
}
