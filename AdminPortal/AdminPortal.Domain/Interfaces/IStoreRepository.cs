using AdminPortal.Domain.Entities;

namespace AdminPortal.Domain.Interfaces;

public interface IStoreRepository : IRepository<Store>
{
    Task<Store?> GetCurrentStoreAsync();
    Task<Store> UpdateStoreStatusAsync(Guid id, bool isOpen);
}
