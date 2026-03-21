using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;
using AdminPortal.Infrastructure.MockData;

namespace AdminPortal.Infrastructure.Repositories;

public class MockDiscountRepository : IDiscountRepository
{
    public Task<Discount?> GetByIdAsync(Guid id) =>
        Task.FromResult(MockDataStore.Discounts.FirstOrDefault(d => d.Id == id));

    public Task<IEnumerable<Discount>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Discount>>(MockDataStore.Discounts.OrderByDescending(d => d.CreatedAt));

    public Task<Discount> AddAsync(Discount entity)
    {
        MockDataStore.Discounts.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<Discount> UpdateAsync(Discount entity)
    {
        var index = MockDataStore.Discounts.FindIndex(d => d.Id == entity.Id);
        if (index >= 0) MockDataStore.Discounts[index] = entity;
        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var discount = MockDataStore.Discounts.FirstOrDefault(d => d.Id == id);
        if (discount is null) return Task.FromResult(false);
        MockDataStore.Discounts.Remove(discount);
        return Task.FromResult(true);
    }

    public Task<Discount?> GetByCodeAsync(string code) =>
        Task.FromResult(MockDataStore.Discounts.FirstOrDefault(d =>
            d.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));

    public Task<IEnumerable<Discount>> GetActiveDiscountsAsync() =>
        Task.FromResult<IEnumerable<Discount>>(MockDataStore.Discounts.Where(d => d.IsActive));
}
