using Microsoft.AspNetCore.Mvc;
using UserManagement.Filters;
using UserManagement.Services;
using UserManagement.ViewModels;

namespace UserManagement.Controllers
{
    [RequireLogin]
    public class ModulesController : Controller
    {
        private readonly IModuleService _modules;

        public ModulesController(IModuleService modules)
        {
            _modules = modules;
        }

        [HttpGet]
        public async Task<IActionResult> Index(SearchFilterViewModel filter)
        {
            filter.Page     = Math.Max(1, filter.Page);
            filter.PageSize = filter.PageSize > 0 ? filter.PageSize : 10;
            var list = await _modules.GetPagedModulesAsync(filter);
            ViewBag.Filter = filter;
            return View(list);
        }

        [HttpGet]
        [RequirePermission("Create", "Modules")]
        public async Task<IActionResult> Create()
        {
            var vm = await _modules.GetModuleForEditAsync(0);
            return View("CreateEdit", vm);
        }

        [HttpGet]
        [RequirePermission("Edit", "Modules")]
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _modules.GetModuleForEditAsync(id);
            if (vm == null) return NotFound();
            return View("CreateEdit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(ModuleCreateEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View("CreateEdit", model);

            var (success, message) = model.Id == 0
                ? await _modules.CreateModuleAsync(model)
                : await _modules.UpdateModuleAsync(model);

            if (!success)
            {
                ModelState.AddModelError("", message);
                return View("CreateEdit", model);
            }

            TempData["Success"] = message;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequirePermission("Delete", "Modules")]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, message) = await _modules.DeleteModuleAsync(id);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction("Index");
        }
    }
}
