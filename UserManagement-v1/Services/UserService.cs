using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.ViewModels;
using X.PagedList;
using X.PagedList.EF;

namespace UserManagement.Services
{
    public interface IUserService
    {
        Task<IPagedList<UserListViewModel>> GetPagedUsersAsync(SearchFilterViewModel filter);
        Task<UserCreateEditViewModel?> GetUserForEditAsync(int id);
        Task<UserDetailViewModel?> GetUserDetailAsync(int id);
        Task<(bool Success, string Message)> CreateUserAsync(UserCreateEditViewModel model);
        Task<(bool Success, string Message)> UpdateUserAsync(UserCreateEditViewModel model);
        Task<(bool Success, string Message)> DeleteUserAsync(int id);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _db;

        public UserService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IPagedList<UserListViewModel>> GetPagedUsersAsync(SearchFilterViewModel filter)
        {
            var query = _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term) ||
                    u.Username.ToLower().Contains(term));
            }

            if (filter.StatusFilter == "active")   query = query.Where(u => u.IsActive);
            if (filter.StatusFilter == "inactive") query = query.Where(u => !u.IsActive);

            query = (filter.SortColumn?.ToLower(), filter.SortOrder?.ToLower()) switch
            {
                ("email",     "desc") => query.OrderByDescending(u => u.Email),
                ("email",      _)     => query.OrderBy(u => u.Email),
                ("username",  "desc") => query.OrderByDescending(u => u.Username),
                ("username",   _)     => query.OrderBy(u => u.Username),
                ("createdat", "desc") => query.OrderByDescending(u => u.CreatedAt),
                ("createdat",  _)     => query.OrderBy(u => u.CreatedAt),
                ("fullname",  "desc") => query.OrderByDescending(u => u.FullName),
                _                     => query.OrderBy(u => u.FullName)
            };

            var projected = query.Select(u => new UserListViewModel
            {
                Id          = u.Id,
                FullName    = u.FullName,
                Email       = u.Email,
                Username    = u.Username,
                IsActive    = u.IsActive,
                CreatedAt   = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                Roles       = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            });

            return await projected.ToPagedListAsync(filter.Page, filter.PageSize);
        }

        public async Task<UserCreateEditViewModel?> GetUserForEditAsync(int id)
        {
            var user = await _db.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return null;

            var allRoles = await _db.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();

            return new UserCreateEditViewModel
            {
                Id              = user.Id,
                FullName        = user.FullName,
                Email           = user.Email,
                Username        = user.Username,
                IsActive        = user.IsActive,
                SelectedRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList(),
                AvailableRoles  = allRoles.Select(r => new RoleCheckboxItem
                {
                    Id         = r.Id,
                    Name       = r.Name,
                    IsSelected = user.UserRoles.Any(ur => ur.RoleId == r.Id)
                }).ToList()
            };
        }

        public async Task<UserDetailViewModel?> GetUserDetailAsync(int id)
        {
            var user = await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RoleModulePermissions)
                            .ThenInclude(rmp => rmp.Module)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return null;

            var perms = user.UserRoles
                .SelectMany(ur => ur.Role.RoleModulePermissions)
                .Select(rmp => new RolePermissionSummary
                {
                    RoleName   = rmp.Role.Name,
                    ModuleName = rmp.Module.Name,
                    CanView    = rmp.CanView,
                    CanCreate  = rmp.CanCreate,
                    CanEdit    = rmp.CanEdit,
                    CanDelete  = rmp.CanDelete
                })
                .ToList();

            return new UserDetailViewModel
            {
                Id          = user.Id,
                FullName    = user.FullName,
                Email       = user.Email,
                Username    = user.Username,
                IsActive    = user.IsActive,
                CreatedAt   = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles       = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                Permissions = perms
            };
        }

        public async Task<(bool Success, string Message)> CreateUserAsync(UserCreateEditViewModel model)
        {
            if (await _db.Users.AnyAsync(u => u.Email == model.Email))
                return (false, "Email already exists.");

            if (await _db.Users.AnyAsync(u => u.Username == model.Username))
                return (false, "Username already exists.");

            if (string.IsNullOrEmpty(model.Password))
                return (false, "Password is required for new users.");

            var user = new User
            {
                FullName     = model.FullName,
                Email        = model.Email,
                Username     = model.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                IsActive     = model.IsActive,
                CreatedAt    = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            foreach (var roleId in model.SelectedRoleIds)
                _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });

            await _db.SaveChangesAsync();
            return (true, "User created successfully.");
        }

        public async Task<(bool Success, string Message)> UpdateUserAsync(UserCreateEditViewModel model)
        {
            var user = await _db.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == model.Id);

            if (user == null) return (false, "User not found.");

            if (await _db.Users.AnyAsync(u => u.Email == model.Email && u.Id != model.Id))
                return (false, "Email already in use.");

            if (await _db.Users.AnyAsync(u => u.Username == model.Username && u.Id != model.Id))
                return (false, "Username already in use.");

            user.FullName  = model.FullName;
            user.Email     = model.Email;
            user.Username  = model.Username;
            user.IsActive  = model.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(model.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            _db.UserRoles.RemoveRange(user.UserRoles);

            foreach (var roleId in model.SelectedRoleIds)
                _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });

            await _db.SaveChangesAsync();
            return (true, "User updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteUserAsync(int id)
        {
            var user = await _db.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return (false, "User not found.");
            if (id == 1)      return (false, "Cannot delete the default super admin account.");

            _db.UserRoles.RemoveRange(user.UserRoles);
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return (true, "User deleted successfully.");
        }
    }
}
