using AdminPortal.Domain.Entities;

namespace AdminPortal.Domain.Interfaces;

public interface IStaffRepository : IRepository<StaffAccount>
{
    Task<IEnumerable<StaffAccount>> GetActiveStaffAsync();
    Task<StaffAccount?> GetByEmailAsync(string email);
}
