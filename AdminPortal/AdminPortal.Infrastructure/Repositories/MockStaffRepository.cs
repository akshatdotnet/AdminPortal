using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;
using AdminPortal.Infrastructure.MockData;

namespace AdminPortal.Infrastructure.Repositories;

public class MockStaffRepository : IStaffRepository
{
    public Task<StaffAccount?> GetByIdAsync(Guid id) =>
        Task.FromResult(MockDataStore.Staff.FirstOrDefault(s => s.Id == id));

    public Task<IEnumerable<StaffAccount>> GetAllAsync() =>
        Task.FromResult<IEnumerable<StaffAccount>>(MockDataStore.Staff.OrderBy(s => s.Name));

    public Task<StaffAccount> AddAsync(StaffAccount entity)
    {
        MockDataStore.Staff.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<StaffAccount> UpdateAsync(StaffAccount entity)
    {
        var index = MockDataStore.Staff.FindIndex(s => s.Id == entity.Id);
        if (index >= 0) MockDataStore.Staff[index] = entity;
        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var staff = MockDataStore.Staff.FirstOrDefault(s => s.Id == id);
        if (staff is null) return Task.FromResult(false);
        MockDataStore.Staff.Remove(staff);
        return Task.FromResult(true);
    }

    public Task<IEnumerable<StaffAccount>> GetActiveStaffAsync() =>
        Task.FromResult<IEnumerable<StaffAccount>>(MockDataStore.Staff.Where(s => s.IsActive));

    public Task<StaffAccount?> GetByEmailAsync(string email) =>
        Task.FromResult(MockDataStore.Staff.FirstOrDefault(s =>
            s.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
}
