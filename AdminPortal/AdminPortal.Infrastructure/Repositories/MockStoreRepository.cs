using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;
using AdminPortal.Infrastructure.MockData;

namespace AdminPortal.Infrastructure.Repositories;

/// <summary>
/// Mock implementation of IStoreRepository using in-memory data.
/// To switch to a real API: implement IStoreRepository against your HTTP client — zero other changes needed.
/// </summary>
public class MockStoreRepository : IStoreRepository
{
    public Task<Store?> GetByIdAsync(Guid id) =>
        Task.FromResult(MockDataStore.Stores.FirstOrDefault(s => s.Id == id));

    public Task<IEnumerable<Store>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Store>>(MockDataStore.Stores);

    public Task<Store> AddAsync(Store entity)
    {
        MockDataStore.Stores.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<Store> UpdateAsync(Store entity)
    {
        var index = MockDataStore.Stores.FindIndex(s => s.Id == entity.Id);
        if (index >= 0) MockDataStore.Stores[index] = entity;
        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var store = MockDataStore.Stores.FirstOrDefault(s => s.Id == id);
        if (store is null) return Task.FromResult(false);
        MockDataStore.Stores.Remove(store);
        return Task.FromResult(true);
    }

    public Task<Store?> GetCurrentStoreAsync() =>
        Task.FromResult(MockDataStore.Stores.FirstOrDefault());

    public Task<Store> UpdateStoreStatusAsync(Guid id, bool isOpen)
    {
        var store = MockDataStore.Stores.First(s => s.Id == id);
        store.IsOpen = isOpen;
        store.UpdatedAt = DateTime.UtcNow;
        return Task.FromResult(store);
    }
}
