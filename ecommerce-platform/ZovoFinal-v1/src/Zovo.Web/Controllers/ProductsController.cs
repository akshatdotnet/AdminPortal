using Microsoft.AspNetCore.Mvc;
using Zovo.Application.Products;

namespace Zovo.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IProductService _svc;
    public ProductsController(IProductService svc) => _svc = svc;

    // GET /Products
    public async Task<IActionResult> Index(
        string? search, string? category, string? status,
        string sortBy = "name_asc", int page = 1)
    {
        var q = new ProductQueryParams {
            Search = search, Category = category,
            Status = status, SortBy = sortBy, Page = page
        };
        var result = await _svc.GetPagedAsync(q);
        ViewData["Search"]     = search;
        ViewData["Category"]   = category;
        ViewData["Status"]     = status;
        ViewData["SortBy"]     = sortBy;
        ViewData["Categories"] = await _svc.GetCategoriesAsync();
        return View(result);
    }

    // GET /Products/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var dto = await _svc.GetDetailAsync(id);
        if (dto is null) return NotFound();
        return View(dto);
    }

    // GET /Products/Create
    public async Task<IActionResult> Create()
    {
        ViewData["Categories"] = await _svc.GetCategoriesAsync();
        return View("Form", new CreateProductCommand { IsActive = true });
    }

    // POST /Products/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductCommand cmd)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Categories"] = await _svc.GetCategoriesAsync();
            return View("Form", cmd);
        }
        var result = await _svc.CreateAsync(cmd);
        TempData["Alert"] = result.IsSuccess
            ? $"success|{result.Message}"
            : $"error|{result.Message}";
        return RedirectToAction(nameof(Index));
    }

    // GET /Products/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _svc.GetDetailAsync(id);
        if (dto is null) return NotFound();
        var cmd = new UpdateProductCommand {
            Id = dto.Id, Name = dto.Name, SKU = dto.SKU,
            Category = dto.Category, SubCategory = dto.SubCategory,
            Price = dto.Price, CompareAtPrice = dto.CompareAtPrice,
            CostPrice = dto.CostPrice, Stock = dto.Stock,
            LowStockThreshold = dto.LowStockThreshold,
            IsActive = dto.IsActive, IsFeatured = dto.IsFeatured,
            ImageUrl = dto.ImageUrl, Description = dto.Description,
            Tags = dto.Tags, Weight = dto.Weight
        };
        ViewData["Id"]         = id;
        ViewData["Categories"] = await _svc.GetCategoriesAsync();
        return View("Form", cmd);
    }

    // POST /Products/Edit/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateProductCommand cmd)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Id"]         = cmd.Id;
            ViewData["Categories"] = await _svc.GetCategoriesAsync();
            return View("Form", cmd);
        }
        var result = await _svc.UpdateAsync(cmd);
        TempData["Alert"] = result.IsSuccess
            ? $"success|{result.Message}"
            : $"error|{result.Message}";
        return RedirectToAction(nameof(Index));
    }

    // POST /Products/Delete/5  (AJAX)
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _svc.DeleteAsync(id);
        return Json(new { success = result.IsSuccess, message = result.Message });
    }

    // POST /Products/Toggle/5  (AJAX)
    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var result = await _svc.ToggleStatusAsync(id);
        return Json(new { success = result.IsSuccess, message = result.Message });
    }
}
