using Microsoft.AspNetCore.Authorization;
using AdminPortal.Application.DTOs;
using AdminPortal.Application.Interfaces;
using AdminPortal.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Web.Controllers;

[Authorize]
public class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 8, string? search = null, string? category = null)
    {
        var result = await _productService.GetProductsAsync(page, pageSize, search, category);
        var categoriesResult = await _productService.GetCategoriesAsync();
        var allResult = await _productService.GetProductsAsync(1, 1000);

        var vm = new ProductListViewModel
        {
            Products = result.Data!,
            SearchQuery = search,
            SelectedCategory = category,
            Categories = categoriesResult.Data?.ToList() ?? new(),
            TotalActive = allResult.Data!.Items.Count(p => p.IsActive),
            TotalOutOfStock = allResult.Data!.Items.Count(p => p.Stock == 0)
        };
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var categoriesResult = await _productService.GetCategoriesAsync();
        return View(new ProductFormViewModel { Categories = categoriesResult.Data?.ToList() ?? new() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        if (!ModelState.IsValid)
            return View(new ProductFormViewModel { Categories = new() });

        var result = await _productService.CreateProductAsync(dto);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Product created successfully!";
            return RedirectToAction(nameof(Index));
        }
        ModelState.AddModelError("", result.ErrorMessage ?? "Failed to create product.");
        return View(new ProductFormViewModel { Categories = new() });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _productService.DeleteProductAsync(id);
        TempData["Success"] = "Product deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(Guid id, bool isActive)
    {
        var result = await _productService.GetProductByIdAsync(id);
        if (result.IsSuccess)
        {
            var update = new UpdateProductDto
            {
                Id = result.Data!.Id,
                Name = result.Data.Name,
                Description = result.Data.Description,
                Price = result.Data.Price,
                DiscountedPrice = result.Data.DiscountedPrice,
                Stock = result.Data.Stock,
                Category = result.Data.Category,
                ImageUrl = result.Data.ImageUrl,
                IsActive = isActive
            };
            await _productService.UpdateProductAsync(update);
        }
        return RedirectToAction(nameof(Index));
    }
}
