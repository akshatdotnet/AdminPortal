using AdminPortal.Domain.Entities;
using AdminPortal.Domain.Interfaces;
using AdminPortal.Infrastructure.MockData;

namespace AdminPortal.Infrastructure.Repositories;

public class MockUserRepository : IUserRepository
{
    public Task<AppUser?> GetByIdAsync(Guid id) =>
        Task.FromResult(MockUserStore.Users.FirstOrDefault(u => u.Id == id));

    public Task<AppUser?> GetByEmailAsync(string email) =>
        Task.FromResult(MockUserStore.Users.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<AppUser?> GetByEmailOrMobileAsync(string emailOrMobile) =>
        Task.FromResult(MockUserStore.Users.FirstOrDefault(u =>
            u.Email.Equals(emailOrMobile, StringComparison.OrdinalIgnoreCase) ||
            u.MobileNumber.Replace(" ", "").Replace("-", "")
             .Equals(emailOrMobile.Replace(" ", "").Replace("-", ""), StringComparison.OrdinalIgnoreCase)));

    public Task<AppUser> AddAsync(AppUser user)
    {
        MockUserStore.Users.Add(user);
        return Task.FromResult(user);
    }

    public Task<AppUser> UpdateAsync(AppUser user)
    {
        var index = MockUserStore.Users.FindIndex(u => u.Id == user.Id);
        if (index >= 0) MockUserStore.Users[index] = user;
        return Task.FromResult(user);
    }
}
