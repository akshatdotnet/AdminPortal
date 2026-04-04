using Microsoft.AspNetCore.Mvc;
using UserManagement.Filters;
using UserManagement.Services;
using UserManagement.ViewModels;

namespace UserManagement.Controllers
{
    [RequireLogin]
    public class UsersController : Controller
    {
        private readonly IUserService _users;
        private readonly IRoleService _roles;

        public UsersController(IUserService users, IRoleService roles)
        {
            _users = users;
            _roles = roles;
        }

        [HttpGet]
        public async Task<IActionResult> Index(SearchFilterViewModel filter)
        {
            filter.Page     = Math.Max(1, filter.Page);
            filter.PageSize = filter.PageSize > 0 ? filter.PageSize : 10;
            var list = await _users.GetPagedUsersAsync(filter);
            ViewBag.Filter = filter;
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var vm = await _users.GetUserDetailAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpGet]
        [RequirePermission("Create", "Users")]
        public async Task<IActionResult> Create()
        {
            var allRoles = await _roles.GetAllActiveRolesAsync();
            var vm = new UserCreateEditViewModel
            {
                IsActive = true,
                AvailableRoles = allRoles.Select(r => new RoleCheckboxItem
                {
                    Id = r.Id, Name = r.Name, IsSelected = false
                }).ToList()
            };
            return View("CreateEdit", vm);
        }

        [HttpGet]
        [RequirePermission("Edit", "Users")]
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _users.GetUserForEditAsync(id);
            if (vm == null) return NotFound();
            return View("CreateEdit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(UserCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var allRoles = await _roles.GetAllActiveRolesAsync();
                model.AvailableRoles = allRoles.Select(r => new RoleCheckboxItem
                {
                    Id = r.Id, Name = r.Name,
                    IsSelected = model.SelectedRoleIds.Contains(r.Id)
                }).ToList();
                return View("CreateEdit", model);
            }

            var (success, message) = model.Id == 0
                ? await _users.CreateUserAsync(model)
                : await _users.UpdateUserAsync(model);

            if (!success)
            {
                ModelState.AddModelError("", message);
                var allRoles = await _roles.GetAllActiveRolesAsync();
                model.AvailableRoles = allRoles.Select(r => new RoleCheckboxItem
                {
                    Id = r.Id, Name = r.Name,
                    IsSelected = model.SelectedRoleIds.Contains(r.Id)
                }).ToList();
                return View("CreateEdit", model);
            }

            TempData["Success"] = message;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Delete", "Users")]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _users.DeleteUserAsync(id);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction("Index");
        }
    }
}
