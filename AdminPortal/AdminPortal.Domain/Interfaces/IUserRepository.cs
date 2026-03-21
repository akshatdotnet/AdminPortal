using AdminPortal.Domain.Entities;

namespace AdminPortal.Domain.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id);
    Task<AppUser?> GetByEmailAsync(string email);
    Task<AppUser?> GetByEmailOrMobileAsync(string emailOrMobile);
    Task<AppUser> AddAsync(AppUser user);
    Task<AppUser> UpdateAsync(AppUser user);
}
