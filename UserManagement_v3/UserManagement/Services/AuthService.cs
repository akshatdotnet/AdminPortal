using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.ViewModels;

namespace UserManagement.Services
{
    public interface IAuthService
    {
        Task<SessionUserViewModel?> ValidateLoginAsync(string username, string password);
        Task UpdateLastLoginAsync(int userId);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IPermissionService _permService;

        public AuthService(AppDbContext db, IPermissionService permService)
        {
            _db = db;
            _permService = permService;
        }

        public async Task<SessionUserViewModel?> ValidateLoginAsync(string username, string password)
        {
            var user = await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u =>
                    (u.Username == username || u.Email == username) && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            var permissions = await _permService.GetUserPermissionsAsync(user.Id);

            return new SessionUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                Permissions = permissions
            };
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
    }
}
