using Microsoft.AspNetCore.Mvc;
using Zovo.Application.Customers;

namespace Zovo.Web.Controllers;

public class CustomersController : Controller
{
    private readonly ICustomerService _svc;
    public CustomersController(ICustomerService svc) => _svc = svc;

    // GET /Customers
    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        var q = new CustomerQueryParams { Search = search, Status = status, Page = page };
        var result = await _svc.GetPagedAsync(q);
        ViewData["Search"] = search;
        ViewData["Status"] = status;
        return View(result);
    }

    // GET /Customers/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var dto = await _svc.GetDetailAsync(id);
        if (dto is null) return NotFound();
        return View(dto);
    }

    // GET /Customers/Create
    public IActionResult Create() => View("Form", new CreateCustomerCommand());

    // POST /Customers/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCustomerCommand cmd)
    {
        if (!ModelState.IsValid) return View("Form", cmd);
        var result = await _svc.CreateAsync(cmd);
        TempData["Alert"] = result.IsSuccess
            ? $"success|{result.Message}"
            : $"error|{result.Message}";
        return RedirectToAction(nameof(Index));
    }

    // GET /Customers/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _svc.GetDetailAsync(id);
        if (dto is null) return NotFound();
        var cmd = new UpdateCustomerCommand {
            Id = dto.Id, FirstName = dto.FirstName, LastName = dto.LastName,
            Email = dto.Email, Phone = dto.Phone, Notes = dto.Notes
        };
        return View("Form", cmd);
    }

    // POST /Customers/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateCustomerCommand cmd)
    {
        if (!ModelState.IsValid) return View("Form", cmd);
        var result = await _svc.UpdateAsync(cmd);
        TempData["Alert"] = result.IsSuccess
            ? $"success|{result.Message}"
            : $"error|{result.Message}";
        return RedirectToAction(nameof(Index));
    }

    // POST /Customers/Delete/5  (AJAX)
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _svc.DeleteAsync(id);
        return Json(new { success = result.IsSuccess, message = result.Message });
    }

    // POST /Customers/Toggle/5  (AJAX)
    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var result = await _svc.ToggleStatusAsync(id);
        return Json(new { success = result.IsSuccess, message = result.Message });
    }
}
