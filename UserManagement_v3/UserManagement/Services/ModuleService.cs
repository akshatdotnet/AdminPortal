using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.ViewModels;
using X.PagedList;
using X.PagedList.EF;

namespace UserManagement.Services
{
    public interface IModuleService
    {
        Task<IPagedList<ModuleListViewModel>> GetPagedModulesAsync(SearchFilterViewModel filter);
        Task<ModuleCreateEditViewModel?> GetModuleForEditAsync(int id);
        Task<(bool Success, string Message)> CreateModuleAsync(ModuleCreateEditViewModel model);
        Task<(bool Success, string Message)> UpdateModuleAsync(ModuleCreateEditViewModel model);
        Task<(bool Success, string Message)> DeleteModuleAsync(int id);
    }

    public class ModuleService : IModuleService
    {
        private readonly AppDbContext _db;

        public ModuleService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IPagedList<ModuleListViewModel>> GetPagedModulesAsync(SearchFilterViewModel filter)
        {
            var query = _db.Modules.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var t = filter.SearchTerm.ToLower();
                query = query.Where(m =>
                    m.Name.ToLower().Contains(t) ||
                    m.ControllerName.ToLower().Contains(t));
            }

            if (filter.StatusFilter == "active")   query = query.Where(m => m.IsActive);
            if (filter.StatusFilter == "inactive") query = query.Where(m => !m.IsActive);

            var projected = query
                .OrderBy(m => m.SortOrder)
                .Select(m => new ModuleListViewModel
                {
                    Id             = m.Id,
                    Name           = m.Name,
                    Description    = m.Description,
                    ControllerName = m.ControllerName,
                    Icon           = m.Icon,
                    SortOrder      = m.SortOrder,
                    IsActive       = m.IsActive
                });

            return await projected.ToPagedListAsync(filter.Page, filter.PageSize);
        }

        public async Task<ModuleCreateEditViewModel?> GetModuleForEditAsync(int id)
        {
            if (id == 0) return new ModuleCreateEditViewModel { IsActive = true, Icon = "bi-grid" };

            var m = await _db.Modules.FindAsync(id);
            if (m == null) return null;

            return new ModuleCreateEditViewModel
            {
                Id             = m.Id,
                Name           = m.Name,
                Description    = m.Description,
                ControllerName = m.ControllerName,
                Icon           = m.Icon,
                SortOrder      = m.SortOrder,
                IsActive       = m.IsActive
            };
        }

        public async Task<(bool Success, string Message)> CreateModuleAsync(ModuleCreateEditViewModel model)
        {
            _db.Modules.Add(new Models.Module
            {
                Name           = model.Name,
                Description    = model.Description,
                ControllerName = model.ControllerName,
                Icon           = model.Icon,
                SortOrder      = model.SortOrder,
                IsActive       = model.IsActive,
                CreatedAt      = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return (true, "Module created successfully.");
        }

        public async Task<(bool Success, string Message)> UpdateModuleAsync(ModuleCreateEditViewModel model)
        {
            var m = await _db.Modules.FindAsync(model.Id);
            if (m == null) return (false, "Module not found.");

            m.Name           = model.Name;
            m.Description    = model.Description;
            m.ControllerName = model.ControllerName;
            m.Icon           = model.Icon;
            m.SortOrder      = model.SortOrder;
            m.IsActive       = model.IsActive;

            await _db.SaveChangesAsync();
            return (true, "Module updated successfully.");
        }

        public async Task<(bool Success, string Message)> DeleteModuleAsync(int id)
        {
            var m = await _db.Modules
                .Include(x => x.RoleModulePermissions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (m == null) return (false, "Module not found.");

            _db.RoleModulePermissions.RemoveRange(m.RoleModulePermissions);
            _db.Modules.Remove(m);
            await _db.SaveChangesAsync();
            return (true, "Module deleted successfully.");
        }
    }
}
