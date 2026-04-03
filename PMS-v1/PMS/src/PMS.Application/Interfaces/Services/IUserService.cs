using PMS.Domain.Entities;

namespace PMS.Application.Interfaces.Services;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllActiveAsync();
    Task<User?> GetByIdAsync(int id);
}