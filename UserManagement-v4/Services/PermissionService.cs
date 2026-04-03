using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.ViewModels;

namespace UserManagement.Services
{
    public interface IPermissionService
    {
        Task<List<ModulePermissionRow>> GetUserPermissionsAsync(int userId);
    }

    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _db;

        public PermissionService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<ModulePermissionRow>> GetUserPermissionsAsync(int userId)
        {
            var roleIds = await _db.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            var perms = await _db.RoleModulePermissions
                .Include(rmp => rmp.Module)
                .Where(rmp => roleIds.Contains(rmp.RoleId))
                .ToListAsync();

            return perms
                .GroupBy(p => p.ModuleId)
                .Select(g => new ModulePermissionRow
                {
                    ModuleId   = g.Key,
                    ModuleName = g.First().Module.Name,
                    ModuleIcon = g.First().Module.Icon,
                    CanView    = g.Any(p => p.CanView),
                    CanCreate  = g.Any(p => p.CanCreate),
                    CanEdit    = g.Any(p => p.CanEdit),
                    CanDelete  = g.Any(p => p.CanDelete)
                })
                .ToList();
        }
    }
}
