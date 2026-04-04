using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.ViewModels;
using X.PagedList;
using X.PagedList.EF;

namespace UserManagement.Services
{
    public interface IRoleService
    {
        Task<IPagedList<RoleListViewModel>> GetPagedRolesAsync(SearchFilterViewModel filter);
        Task<RoleCreateEditViewModel?> GetRoleForEditAsync(int id);
        Task<(bool Success, string Message)> CreateRoleAsync(RoleCreateEditViewModel model);
        Task<(bool Success, string Message)> UpdateRoleAsync(RoleCreateEditViewModel model);
        Task<(bool Success, string Message)> DeleteRoleAsync(int id);
        Task<List<RoleListViewModel>> GetAllActiveRolesAsync();
    }

    public class RoleService : IRoleService
    {
        private readonly AppDbContext _db;

        public RoleService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IPagedList<RoleListViewModel>> GetPagedRolesAsync(SearchFilterViewModel filter)
        {
            var query = _db.Roles.Include(r => r.UserRoles).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(r =>
                    r.Name.ToLower().Contains(term) ||
                    r.Description.ToLower().Contains(term));
            }

            if (filter.StatusFilter == "active")   query = query.Where(r => r.IsActive);
            if (filter.StatusFilter == "inactive") query = query.Where(r => !r.IsActive);

            var projected = query.Select(r => new RoleListViewModel
            {
                Id          = r.Id,
                Name        = r.Name,
                Description = r.Description,
                IsActive    = r.IsActive,
                UserCount   = r.UserRoles.Count,
                CreatedAt   = r.CreatedAt
            }).OrderBy(r => r.Name);

            return await projected.ToPagedListAsync(filter.Page, filter.PageSize);
        }

        public async Task<RoleCreateEditViewModel?> GetRoleForEditAsync(int id)
        {
            var modules = await _db.Modules
                .Where(m => m.IsActive)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            if (id == 0)
            {
                return new RoleCreateEditViewModel
                {
                    IsActive = true,
                    ModulePermissions = modules.Select(m => new ModulePermissionRow
                    {
                        ModuleId   = m.Id,
                        ModuleName = m.Name,
                        ModuleIcon = m.Icon
                    }).ToList()
                };
            }

            var role = await _db.Roles
                .Include(r => r.RoleModulePermissions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null) return null;

            return new RoleCreateEditViewModel
            {
                Id          = role.Id,
                Name        = role.Name,
                Description = role.Description,
                IsActive    = role.IsActive,
                ModulePermissions = modules.Select(m =>
                {
                    var perm = role.RoleModulePermissions.FirstOrDefault(p => p.ModuleId == m.Id);
                    return new ModulePermissionRow
                    {
                        ModuleId   = m.Id,
                        ModuleName = m.Name,
                        ModuleIcon = m.Icon,
                        CanView    = perm?.CanView   ?? false,
                        CanCreate  = perm?.CanCreate ?? false,
                        CanEdit    = perm?.CanEdit   ?? false,
                        CanDelete  = perm?.CanDelete ?? false
                    };
                }).ToList()
            };
        }

        public async Task<(bool Success, string Message)> CreateRoleAsync(RoleCreateEditViewModel model)
        {
            if (await _db.Roles.AnyAsync(r => r.Name == model.Name))
                return (false, "Role name already exists.");

            var role = new Role
            {
                Name        = model.Name,
                Description = model.Description,
                IsActive    = model.IsActive,
                CreatedAt   = DateTime.UtcNow
            };

            _db.Roles.Add(role);
            await _db.SaveChangesAsync();

            foreach (var perm in model.ModulePermissions)
            {
                _db.RoleModulePermissions.Add(new RoleModulePermission
                {
                    RoleId    = role.Id,
                    ModuleId  = perm.ModuleId,
                    CanView   = perm.CanView,
                    CanCreate = perm.CanCreate,
                    CanEdit   = perm.CanEdit,
                    CanDelete = perm.CanDelete
                });
            }

            await _db.SaveChangesAsync();
            return (true, "Role created successfully.");
        }

        public async Task<(bool Success, string Message)> UpdateRoleAsync(RoleCreateEditViewModel model)
        {
            var role = await _db.Roles
                .Include(r => r.RoleModulePermissions)
                .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (role == null) return (false, "Role not found.");

            if (await _db.Roles.AnyAsync(r => r.Name == model.Name && r.Id != model.Id))
                return (false, "Role name already in use.");

            role.Name        = model.Name;
            role.Description = model.Description;
            role.IsActive    = model.IsActive;

            _db.RoleModulePermissions.RemoveRange(role.RoleModulePermissions);

            foreach (var perm in model.ModulePermissions)
            {
                _db.RoleModulePermissions.Add(new RoleModulePermission
                {
                    RoleId    = role.Id,
                    ModuleId  = perm.ModuleId,
                    CanView   = perm.CanView,
                    CanCreate = perm.CanCreate,
                    CanEdit   = perm.CanEdit,
                    CanDelete = perm.CanDelete
                });
            }

            await _db.SaveChangesAsync();
            return (true, "Role updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteRoleAsync(int id)
        {
            var role = await _db.Roles
                .Include(r => r.UserRoles)
                .Include(r => r.RoleModulePermissions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (role == null) return (false, "Role not found.");

            if (role.UserRoles.Any())
                return (false, $"Cannot delete. {role.UserRoles.Count} user(s) are assigned this role.");

            _db.RoleModulePermissions.RemoveRange(role.RoleModulePermissions);
            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
            return (true, "Role deleted successfully.");
        }

        public async Task<List<RoleListViewModel>> GetAllActiveRolesAsync()
        {
            return await _db.Roles
                .Where(r => r.IsActive)
                .Select(r => new RoleListViewModel { Id = r.Id, Name = r.Name, Description = r.Description })
                .OrderBy(r => r.Name)
                .ToListAsync();
        }
    }
}
