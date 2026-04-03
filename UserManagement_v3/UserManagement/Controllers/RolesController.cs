using Microsoft.AspNetCore.Mvc;
using UserManagement.Filters;
using UserManagement.Services;
using UserManagement.ViewModels;

namespace UserManagement.Controllers
{
    [RequireLogin]
    public class RolesController : Controller
    {
        private readonly IRoleService _roles;

        public RolesController(IRoleService roles)
        {
            _roles = roles;
        }

        [HttpGet]
        public async Task<IActionResult> Index(SearchFilterViewModel filter)
        {
            filter.Page     = Math.Max(1, filter.Page);
            filter.PageSize = filter.PageSize > 0 ? filter.PageSize : 10;
            var list = await _roles.GetPagedRolesAsync(filter);
            ViewBag.Filter = filter;
            return View(list);
        }

        [HttpGet]
        [RequirePermission("Create", "Roles")]
        public async Task<IActionResult> Create()
        {
            var vm = await _roles.GetRoleForEditAsync(0);
            return View("CreateEdit", vm);
        }

        [HttpGet]
        [RequirePermission("Edit", "Roles")]
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _roles.GetRoleForEditAsync(id);
            if (vm == null) return NotFound();
            return View("CreateEdit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(RoleCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var fresh = await _roles.GetRoleForEditAsync(model.Id);
                if (fresh != null) model.ModulePermissions = fresh.ModulePermissions;
                return View("CreateEdit", model);
            }

            var (success, message) = model.Id == 0
                ? await _roles.CreateRoleAsync(model)
                : await _roles.UpdateRoleAsync(model);

            if (!success)
            {
                ModelState.AddModelError("", message);
                var fresh = await _roles.GetRoleForEditAsync(model.Id);
                if (fresh != null) model.ModulePermissions = fresh.ModulePermissions;
                return View("CreateEdit", model);
            }

            TempData["Success"] = message;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Delete", "Roles")]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _roles.DeleteRoleAsync(id);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction("Index");
        }
    }
}
